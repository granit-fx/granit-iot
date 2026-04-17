using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws;
using Granit.IoT.Ingestion.Aws.Internal.SigV4;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using ZiggyCreatures.Caching.Fusion;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal.SigV4;

public class DefaultSigV4RequestValidatorTests
{
    // get-vanilla test vector from AWS SigV4 test suite.
    private const string AccessKeyId = "AKIDEXAMPLE";
    private const string SecretAccessKey = "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY";
    private const string AmzDate = "20150830T123600Z";
    private static readonly DateTimeOffset RequestTime =
        new(2015, 8, 30, 12, 36, 0, TimeSpan.Zero);

    private const string ValidAuthorization =
        "AWS4-HMAC-SHA256 " +
        "Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, " +
        "SignedHeaders=host;x-amz-date, " +
        "Signature=5fa00fa31553b73ebf1942676e86291e8372ff2a2260956d9b8aae1d763fbf31";

    [Fact]
    public async Task Accepts_the_get_vanilla_AWS_test_vector()
    {
        Fixture fx = new();
        Dictionary<string, string> headers = BuildHeaders(ValidAuthorization);

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            "GET", "/", string.Empty, headers, ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue(customMessage: result.FailureReason);
    }

    [Fact]
    public async Task Rejects_a_tampered_signature()
    {
        Fixture fx = new();
        string tamperedAuth = ValidAuthorization.Replace(
            "Signature=5fa00fa3", "Signature=5fa00fa0", StringComparison.Ordinal);

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            "GET", "/", string.Empty,
            BuildHeaders(tamperedAuth), ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("signature mismatch");
    }

    [Fact]
    public async Task Rejects_when_Authorization_header_is_absent()
    {
        Fixture fx = new();
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = "example.amazonaws.com",
            ["x-amz-date"] = AmzDate,
        };

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            "GET", "/", string.Empty, headers, ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("Missing Authorization");
    }

    [Fact]
    public async Task Rejects_when_x_amz_date_is_outside_the_clock_skew_window()
    {
        Fixture fx = new(clockNowUtc: RequestTime.AddMinutes(10));

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            "GET", "/", string.Empty,
            BuildHeaders(ValidAuthorization), ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("clock-skew");
    }

    [Fact]
    public async Task Rejects_when_credential_scope_date_does_not_match_x_amz_date()
    {
        Fixture fx = new();
        string mismatchedAuth = ValidAuthorization.Replace(
            "Credential=AKIDEXAMPLE/20150830/",
            "Credential=AKIDEXAMPLE/20150829/",
            StringComparison.Ordinal);

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            "GET", "/", string.Empty,
            BuildHeaders(mismatchedAuth), ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("Credential scope date");
    }

    [Fact]
    public async Task Rejects_when_access_key_id_is_unknown()
    {
        Fixture fx = new(knownKeyId: "DIFFERENT-KEY");

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            "GET", "/", string.Empty,
            BuildHeaders(ValidAuthorization), ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("Unknown access key id");
    }

    [Fact]
    public async Task Rejects_when_x_amz_date_header_is_missing()
    {
        Fixture fx = new();
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = "example.amazonaws.com",
            ["authorization"] = ValidAuthorization,
        };

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            "GET", "/", string.Empty, headers, ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("x-amz-date");
    }

    private static Dictionary<string, string> BuildHeaders(string authorization) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = "example.amazonaws.com",
            ["x-amz-date"] = AmzDate,
            ["authorization"] = authorization,
        };

    private sealed class Fixture
    {
        public Fixture(DateTimeOffset? clockNowUtc = null, string knownKeyId = AccessKeyId)
        {
            ISigV4SigningKeyProvider keyProvider = Substitute.For<ISigV4SigningKeyProvider>();
            keyProvider.GetSecretAccessKeyAsync(knownKeyId, Arg.Any<CancellationToken>())
                .Returns(SecretAccessKey);
            keyProvider
                .GetSecretAccessKeyAsync(Arg.Is<string>(s => s != knownKeyId), Arg.Any<CancellationToken>())
                .Returns((string?)null);

            IFusionCache cache = Substitute.For<IFusionCache>();
            cache.GetOrSetAsync<byte[]>(
                    Arg.Any<string>(),
                    Arg.Any<Func<FusionCacheFactoryExecutionContext<byte[]>, CancellationToken, Task<byte[]>>>(),
                    Arg.Any<MaybeValue<byte[]>>(),
                    Arg.Any<FusionCacheEntryOptions>(),
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo => SigV4SigningKey.Derive(
                    SecretAccessKey,
                    new SigV4Scope("20150830", "us-east-1", "service")));

            IOptionsMonitor<AwsIoTIngestionOptions> optionsMonitor =
                Substitute.For<IOptionsMonitor<AwsIoTIngestionOptions>>();
            optionsMonitor.CurrentValue.Returns(new AwsIoTIngestionOptions
            {
                SigningKeyCacheHours = 24,
                Direct = { Enabled = true, Region = "us-east-1" },
            });

            FakeTimeProvider timeProvider = new();
            timeProvider.SetUtcNow(clockNowUtc ?? RequestTime);

            Validator = new DefaultSigV4RequestValidator(
                keyProvider, cache, optionsMonitor, timeProvider);
        }

        public DefaultSigV4RequestValidator Validator { get; }
    }
}
