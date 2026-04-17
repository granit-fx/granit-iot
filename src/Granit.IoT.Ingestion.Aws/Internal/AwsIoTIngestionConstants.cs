namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Source-name constants for the three AWS IoT Core ingestion paths. These values
/// are matched against the <c>{source}</c> route parameter of
/// <c>POST /iot/ingest/{source}</c> to dispatch the right validator and parser.
/// Lowercase per framework convention.
/// </summary>
internal static class AwsIoTIngestionConstants
{
    /// <summary>IoT Rules → SNS → HTTP subscription path.</summary>
    internal const string SnsSourceName = "awsiotsns";

    /// <summary>IoT Rules → HTTP direct path (Bearer API key or SigV4).</summary>
    internal const string DirectSourceName = "awsiotdirect";

    /// <summary>IoT Rules → API Gateway → HTTP path (SigV4).</summary>
    internal const string ApiGatewaySourceName = "awsiotapigw";
}
