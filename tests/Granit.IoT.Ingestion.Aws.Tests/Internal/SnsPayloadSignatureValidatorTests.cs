using System.Security.Cryptography;
using System.Text;
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

namespace Granit.IoT.Ingestion.Aws.Tests.Internal;

public class SnsPayloadSignatureValidatorTests
{
    private static readonly IReadOnlyDictionary<string, string> NoHeaders =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public async Task Validates_well_formed_Notification_with_correct_signature()
    {
        using ValidatorFixture fixture = new();
        byte[] envelope = fixture.BuildSignedNotification(messageId: "msg-1");

        SignatureValidationResult result = await fixture.Validator.ValidateAsync(
            envelope, NoHeaders, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue(customMessage: result.FailureReason);
    }

    [Fact]
    public async Task Tampered_Message_field_fails_signature_verification()
    {
        using ValidatorFixture fixture = new();
        byte[] signed = fixture.BuildSignedNotification(messageId: "msg-2");

        // Flip one character in the Message field — RSA verification must fail.
        string json = Encoding.UTF8.GetString(signed);
        byte[] tampered = Encoding.UTF8.GetBytes(json.Replace(
            "\"Message\":\"hello\"",
            "\"Message\":\"hellx\"",
            StringComparison.Ordinal));

        SignatureValidationResult result = await fixture.Validator.ValidateAsync(
            tampered, NoHeaders, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("signature verification failed");
    }

    [Fact]
    public async Task Rejects_TopicArn_that_does_not_match_configured_prefix()
    {
        using ValidatorFixture fixture = new(topicArnPrefix: "arn:aws:sns:eu-west-1:1:iot-");
        byte[] signed = fixture.BuildSignedNotification(
            messageId: "msg-3",
            topicArn: "arn:aws:sns:us-east-1:9:other");

        SignatureValidationResult result = await fixture.Validator.ValidateAsync(
            signed, NoHeaders, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("does not match the configured prefix");
    }

    [Fact]
    public async Task Second_delivery_of_same_MessageId_is_treated_as_replay()
    {
        using ValidatorFixture fixture = new();
        byte[] first = fixture.BuildSignedNotification(messageId: "replay-1");

        SignatureValidationResult firstResult = await fixture.Validator.ValidateAsync(
            first, NoHeaders, TestContext.Current.CancellationToken);
        firstResult.IsValid.ShouldBeTrue();

        byte[] second = fixture.BuildSignedNotification(messageId: "replay-1");
        SignatureValidationResult secondResult = await fixture.Validator.ValidateAsync(
            second, NoHeaders, TestContext.Current.CancellationToken);

        secondResult.IsValid.ShouldBeFalse();
        secondResult.FailureReason!.ShouldContain("already seen");
    }

    [Fact]
    public async Task Malformed_envelope_JSON_is_rejected_cleanly()
    {
        using ValidatorFixture fixture = new();
        byte[] garbage = Encoding.UTF8.GetBytes("{not really json");

        SignatureValidationResult result = await fixture.Validator.ValidateAsync(
            garbage, NoHeaders, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("not valid JSON");
    }

    [Fact]
    public async Task When_SNS_path_is_disabled_validation_fails_fast()
    {
        using ValidatorFixture fixture = new(snsEnabled: false);
        byte[] signed = fixture.BuildSignedNotification(messageId: "msg-off");

        SignatureValidationResult result = await fixture.Validator.ValidateAsync(
            signed, NoHeaders, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("disabled");
    }

    private sealed class ValidatorFixture : IDisposable
    {
        private const string SigningCertUrl =
            "https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-abc.pem";

        private readonly RSA _rsa;
        private readonly HashSet<string> _seenDedupKeys = [];

        public ValidatorFixture(bool snsEnabled = true, string? topicArnPrefix = null)
        {
            _rsa = RSA.Create(2048);

            ISnsSigningCertificateCache certCache = Substitute.For<ISnsSigningCertificateCache>();
            certCache.GetPublicKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(_rsa));

            IOptionsMonitor<AwsIoTIngestionOptions> optionsMonitor =
                Substitute.For<IOptionsMonitor<AwsIoTIngestionOptions>>();
            optionsMonitor.CurrentValue.Returns(new AwsIoTIngestionOptions
            {
                Sns = new SnsIngestionOptions
                {
                    Enabled = snsEnabled,
                    Region = "eu-west-1",
                    TopicArnPrefix = topicArnPrefix,
                    DeduplicationWindowMinutes = 5,
                    CertCacheHours = 24,
                    AutoConfirmSubscription = false,
                },
            });

            IFusionCache dedupCache = Substitute.For<IFusionCache>();
            dedupCache.TryGetAsync<byte>(
                    Arg.Any<string>(),
                    Arg.Any<FusionCacheEntryOptions>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo => _seenDedupKeys.Contains(callInfo.ArgAt<string>(0))
                    ? MaybeValue<byte>.FromValue(1)
                    : MaybeValue<byte>.None);
#pragma warning disable CA2012 // NSubstitute records the matcher; the ValueTask returned by SetAsync is intentionally discarded.
            dedupCache.When(c => c.SetAsync<byte>(
                    Arg.Any<string>(),
                    Arg.Any<byte>(),
                    Arg.Any<FusionCacheEntryOptions>(),
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<CancellationToken>()))
                .Do(callInfo => _seenDedupKeys.Add(callInfo.ArgAt<string>(0)));
#pragma warning restore CA2012

            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

            Metrics = new AwsIoTIngestionMetrics();
            Validator = new SnsPayloadSignatureValidator(
                certCache,
                optionsMonitor,
                dedupCache,
                httpClientFactory,
                Metrics,
                NullLogger<SnsPayloadSignatureValidator>.Instance);
        }

        public SnsPayloadSignatureValidator Validator { get; }

        public AwsIoTIngestionMetrics Metrics { get; }

        public byte[] BuildSignedNotification(
            string messageId,
            string topicArn = "arn:aws:sns:eu-west-1:1:iot-telemetry")
        {
            SnsEnvelope envelope = new()
            {
                Type = SnsEnvelope.MessageTypes.Notification,
                MessageId = messageId,
                TopicArn = topicArn,
                Message = "hello",
                Timestamp = "2026-04-17T12:00:00.000Z",
                SignatureVersion = "1",
                SigningCertUrl = SigningCertUrl,
            };

            string canonical = SnsCanonicalStringBuilder.Build(envelope)!;
            byte[] sig = _rsa.SignData(
                Encoding.UTF8.GetBytes(canonical),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            envelope.Signature = Convert.ToBase64String(sig);

            return JsonSerializer.SerializeToUtf8Bytes(envelope);
        }

        public void Dispose()
        {
            _rsa.Dispose();
            Metrics.Dispose();
        }
    }
}
