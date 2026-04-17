using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws;
using Granit.IoT.Ingestion.Aws.Diagnostics;
using Granit.IoT.Ingestion.Aws.Internal;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal;

public class ApiGatewayPayloadSignatureValidatorTests
{
    [Fact]
    public async Task Disabled_path_fails_fast_without_calling_SigV4()
    {
        Fixture fx = new(enabled: false);

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
    public async Task Missing_server_controlled_metadata_is_rejected()
    {
        Fixture fx = new();

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeFalse();
        result.FailureReason!.ShouldContain("granit-request-");
    }

    [Fact]
    public async Task Delegates_to_SigV4_validator_when_metadata_is_present()
    {
        Fixture fx = new();
        fx.SigV4.ValidateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(SignatureValidationResult.Valid);

        SignatureValidationResult result = await fx.Validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty,
            BuildHeaders(path: "/iot/ingest/awsiotapigw"),
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    private static Dictionary<string, string> BuildHeaders(
        string method = "POST",
        string path = "/iot/ingest/awsiotapigw",
        string query = "") =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [IngestionRequestHeaders.Method] = method,
            [IngestionRequestHeaders.Path] = path,
            [IngestionRequestHeaders.Query] = query,
        };

    private sealed class Fixture
    {
        public Fixture(bool enabled = true)
        {
            SigV4 = Substitute.For<ISigV4RequestValidator>();

            IOptionsMonitor<AwsIoTIngestionOptions> optionsMonitor =
                Substitute.For<IOptionsMonitor<AwsIoTIngestionOptions>>();
            optionsMonitor.CurrentValue.Returns(new AwsIoTIngestionOptions
            {
                ApiGateway = new ApiGatewayIngestionOptions
                {
                    Enabled = enabled,
                    Region = "eu-west-1",
                },
            });

            Metrics = new AwsIoTIngestionMetrics();
            Validator = new ApiGatewayPayloadSignatureValidator(SigV4, optionsMonitor, Metrics);
        }

        public ISigV4RequestValidator SigV4 { get; }
        public AwsIoTIngestionMetrics Metrics { get; }
        public ApiGatewayPayloadSignatureValidator Validator { get; }
    }
}
