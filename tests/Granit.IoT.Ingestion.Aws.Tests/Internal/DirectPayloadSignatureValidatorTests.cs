using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws;
using Granit.IoT.Ingestion.Aws.Diagnostics;
using Granit.IoT.Ingestion.Aws.Internal;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal;

public class DirectPayloadSignatureValidatorTests
{
    [Fact]
    public async Task Path_disabled_in_options_fails_fast_without_touching_SigV4()
    {
        Fixture fx = new(enabled: false, authMode: DirectAuthMode.SigV4);

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty, BuildHeaders(), TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("disabled");
        await fx.SigV4.DidNotReceive().ValidateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SigV4_mode_requires_server_controlled_request_metadata_headers()
    {
        Fixture fx = new(authMode: DirectAuthMode.SigV4);
        Dictionary<string, string> headersWithoutMetadata = new(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = "example.com",
            ["x-amz-date"] = "20260417T120000Z",
        };

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty, headersWithoutMetadata, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("granit-request-");
    }

    [Fact]
    public async Task SigV4_mode_delegates_to_ISigV4RequestValidator_with_header_metadata()
    {
        Fixture fx = new(authMode: DirectAuthMode.SigV4);
        fx.SigV4.ValidateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(SignatureValidationResult.Valid);

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty,
            BuildHeaders(method: "POST", path: "/iot/ingest/awsiotdirect", query: "id=42"),
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
        await fx.SigV4.Received(1).ValidateAsync(
            "POST", "/iot/ingest/awsiotdirect", "id=42",
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApiKey_mode_with_no_configured_key_fails()
    {
        Fixture fx = new(authMode: DirectAuthMode.ApiKey, apiKey: null);

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty,
            BuildHeaders(authorization: "Bearer abc"),
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("no ApiKey is configured");
    }

    [Fact]
    public async Task ApiKey_mode_accepts_correct_bearer_token()
    {
        Fixture fx = new(authMode: DirectAuthMode.ApiKey, apiKey: "secret-123");

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty,
            BuildHeaders(authorization: "Bearer secret-123"),
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ApiKey_mode_rejects_wrong_bearer_token()
    {
        Fixture fx = new(authMode: DirectAuthMode.ApiKey, apiKey: "secret-123");

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty,
            BuildHeaders(authorization: "Bearer wrong"),
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("mismatch");
    }

    [Fact]
    public async Task ApiKey_mode_rejects_missing_Authorization_header()
    {
        Fixture fx = new(authMode: DirectAuthMode.ApiKey, apiKey: "secret-123");

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty, BuildHeaders(), TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("Bearer");
    }

    private static Dictionary<string, string> BuildHeaders(
        string? method = "POST",
        string? path = "/iot/ingest/awsiotdirect",
        string? query = "",
        string? authorization = null)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        if (method is not null)
        {
            headers[IngestionRequestHeaders.Method] = method;
        }
        if (path is not null)
        {
            headers[IngestionRequestHeaders.Path] = path;
        }
        if (query is not null)
        {
            headers[IngestionRequestHeaders.Query] = query;
        }
        if (authorization is not null)
        {
            headers["Authorization"] = authorization;
        }
        return headers;
    }

    private sealed class Fixture
    {
        public Fixture(
            bool enabled = true,
            DirectAuthMode authMode = DirectAuthMode.SigV4,
            string? apiKey = null)
        {
            SigV4 = Substitute.For<ISigV4RequestValidator>();

            IOptionsMonitor<AwsIoTIngestionOptions> optionsMonitor =
                Substitute.For<IOptionsMonitor<AwsIoTIngestionOptions>>();
            optionsMonitor.CurrentValue.Returns(new AwsIoTIngestionOptions
            {
                Direct = new AwsIoTDirectIngestionOptions
                {
                    Enabled = enabled,
                    Region = "eu-west-1",
                    AuthMode = authMode,
                    ApiKey = apiKey,
                },
            });

            Metrics = new IoTIngestionAwsMetrics(new TestMeterFactory());
            Validator = new DirectPayloadSignatureValidator(SigV4, optionsMonitor, Metrics);
        }

        public ISigV4RequestValidator SigV4 { get; }
        public IoTIngestionAwsMetrics Metrics { get; }
        public DirectPayloadSignatureValidator Validator { get; }
    }
}
