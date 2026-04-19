using System.Security.Cryptography;
using System.Text.Json;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws;
using Granit.IoT.Ingestion.Aws.Diagnostics;
using Granit.IoT.Ingestion.Aws.Internal;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using ZiggyCreatures.Caching.Fusion;

namespace Granit.IoT.Ingestion.Aws.Tests;

/// <summary>
/// Story #43 — validates that <see cref="SnsPayloadSignatureValidator"/> accepts a
/// pre-built AWS SNS test vector. The envelope, the signing keypair, and the
/// pre-computed RSA-SHA256 signature all live in
/// <c>Fixtures/aws-sns-test-vector.json</c>; the test wires the public key into
/// the <see cref="ISnsSigningCertificateCache"/> mock so no network call is made.
/// </summary>
public sealed class SnsSignatureValidationTests
{
    private const string FixturePath = "Fixtures/aws-sns-test-vector.json";

    private static readonly IReadOnlyDictionary<string, string> NoHeaders =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions FixtureJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Fixture_vector_passes_signature_verification()
    {
        AwsSnsTestVector vector = LoadFixture();

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(vector.SigningKeys.PublicKeyPem);

        using var harness = new ValidatorHarness(publicKey);
        byte[] envelopeJson = JsonSerializer.SerializeToUtf8Bytes(vector.Envelope);

        SignatureValidationResult result = await harness.Validator.ValidateAsync(
            envelopeJson, NoHeaders, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue(customMessage: result.FailureReason);
    }

    [Fact]
    public async Task Fixture_vector_rejects_tampered_message()
    {
        AwsSnsTestVector vector = LoadFixture();

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(vector.SigningKeys.PublicKeyPem);

        using var harness = new ValidatorHarness(publicKey);
        vector.Envelope["Message"] = "hello from AWS test vector — tampered";
        byte[] envelopeJson = JsonSerializer.SerializeToUtf8Bytes(vector.Envelope);

        SignatureValidationResult result = await harness.Validator.ValidateAsync(
            envelopeJson, NoHeaders, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("signature verification failed");
    }

    private static AwsSnsTestVector LoadFixture()
    {
        string json = File.ReadAllText(FixturePath);
        return JsonSerializer.Deserialize<AwsSnsTestVector>(json, FixtureJsonOptions)!;
    }

    private sealed record AwsSnsTestVector(
        Dictionary<string, string> Envelope,
        AwsSnsTestVectorKeys SigningKeys);

    private sealed record AwsSnsTestVectorKeys(string PrivateKeyPem, string PublicKeyPem);

    private sealed class ValidatorHarness : IDisposable
    {
        public ValidatorHarness(RSA publicKey)
        {
            ISnsSigningCertificateCache certCache = Substitute.For<ISnsSigningCertificateCache>();
            certCache.GetPublicKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(publicKey));

            IOptionsMonitor<AwsIoTIngestionOptions> optionsMonitor =
                Substitute.For<IOptionsMonitor<AwsIoTIngestionOptions>>();
            optionsMonitor.CurrentValue.Returns(new AwsIoTIngestionOptions
            {
                Sns = new AwsIoTSnsIngestionOptions
                {
                    Enabled = true,
                    Region = "eu-west-1",
                    DeduplicationWindowMinutes = 5,
                    CertCacheHours = 24,
                },
            });

            IFusionCache dedupCache = Substitute.For<IFusionCache>();
            dedupCache.TryGetAsync<byte>(
                    Arg.Any<string>(),
                    Arg.Any<FusionCacheEntryOptions>(),
                    Arg.Any<CancellationToken>())
                .Returns(MaybeValue<byte>.None);

            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

            Metrics = new IoTIngestionAwsMetrics(new TestMeterFactory());
            Validator = new SnsPayloadSignatureValidator(
                certCache,
                optionsMonitor,
                dedupCache,
                httpClientFactory,
                Metrics,
                NullLogger<SnsPayloadSignatureValidator>.Instance);
        }

        public SnsPayloadSignatureValidator Validator { get; }

        public IoTIngestionAwsMetrics Metrics { get; }

        public void Dispose() => Metrics.Dispose();
    }
}
