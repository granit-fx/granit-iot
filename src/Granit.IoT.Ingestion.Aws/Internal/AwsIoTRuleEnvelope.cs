using System.Text.Json.Serialization;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Shape of the JSON produced by an AWS IoT Rule republishing telemetry to
/// HTTPS. This is the shape authors MUST emit in their rule SQL:
/// <code>
/// SELECT
///     newuuid() AS messageId,
///     clientId() AS deviceId,
///     timestamp() AS timestamp,
///     payload AS metrics
/// FROM 'granit/telemetry/+'
/// </code>
/// The <c>metrics</c> field is expected to be an object mapping metric
/// name → numeric value.
/// </summary>
internal sealed class AwsIoTRuleEnvelope
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// ISO-8601 string, or Unix-epoch milliseconds as a number (IoT Rule
    /// <c>timestamp()</c> returns ms-since-epoch). The parser accepts both.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public System.Text.Json.JsonElement Timestamp { get; set; }

    [JsonPropertyName("metrics")]
    public Dictionary<string, double>? Metrics { get; set; }
}
