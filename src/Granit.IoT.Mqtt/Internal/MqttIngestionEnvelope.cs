using System.Text.Json.Serialization;

namespace Granit.IoT.Mqtt.Internal;

/// <summary>
/// JSON envelope produced by the MQTT bridge before it invokes
/// <c>IIngestionPipeline.ProcessAsync</c>. The pipeline's parser contract
/// (<c>IInboundMessageParser.ParseAsync</c>) only sees the body bytes — there are no
/// headers — so MQTT-specific metadata (topic, QoS, retain, packet ID) must be carried
/// in-band. This mirrors the Scaleway envelope shape so the IoT pipeline stays
/// transport-agnostic.
/// </summary>
internal sealed record MqttIngestionEnvelope
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; init; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; init; } = string.Empty;

    [JsonPropertyName("qos")]
    public int Qos { get; init; }

    [JsonPropertyName("retain")]
    public bool Retain { get; init; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Inner device payload: <c>{ "recordedAt": "...", "metrics": { "temperature": 42.1 }, "tags": { ... } }</c>.
    /// Stored as a raw JSON node (not pre-deserialized) so the parser controls allocation.
    /// </summary>
    [JsonPropertyName("payload")]
    public InnerPayload? Payload { get; init; }
}

internal sealed record InnerPayload
{
    [JsonPropertyName("recordedAt")]
    public DateTimeOffset? RecordedAt { get; init; }

    [JsonPropertyName("metrics")]
    public Dictionary<string, double>? Metrics { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }
}

[JsonSerializable(typeof(MqttIngestionEnvelope))]
[JsonSerializable(typeof(InnerPayload))]
[JsonSerializable(typeof(Dictionary<string, double>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class MqttJsonContext : JsonSerializerContext;
