using System.Text.Json.Serialization;

namespace Granit.IoT.Ingestion.Scaleway.Internal;

/// <summary>
/// JSON envelope sent by Scaleway IoT Hub to webhook subscribers.
/// </summary>
internal sealed record ScalewayEnvelope
{
    [JsonPropertyName("topic")]
    public string Topic { get; init; } = string.Empty;

    [JsonPropertyName("message_id")]
    public string MessageId { get; init; } = string.Empty;

    [JsonPropertyName("payload")]
    public string Payload { get; init; } = string.Empty;

    [JsonPropertyName("qos")]
    public int Qos { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }
}

[JsonSerializable(typeof(ScalewayEnvelope))]
[JsonSerializable(typeof(Dictionary<string, double>))]
internal sealed partial class ScalewayJsonContext : JsonSerializerContext;
