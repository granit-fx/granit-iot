using System.Text.Json;
using Granit.IoT.Ingestion;
using Granit.IoT.Ingestion.Abstractions;

namespace Granit.IoT.Mqtt.Internal;

/// <summary>
/// Parses the <see cref="MqttIngestionEnvelope"/> the MQTT bridge produces into the
/// pipeline's <see cref="ParsedTelemetryBatch"/>. Mirrors the Scaleway envelope flow:
/// the bridge is a transport adapter that wraps the raw device payload with MQTT
/// metadata, and this parser unwraps it.
/// </summary>
internal sealed class MqttMessageParser : IInboundMessageParser
{
    public string SourceName => MqttConstants.SourceName;

    public ValueTask<ParsedTelemetryBatch> ParseAsync(
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        MqttIngestionEnvelope envelope = DeserializeEnvelope(body.Span);

        if (string.IsNullOrWhiteSpace(envelope.MessageId))
        {
            throw new IngestionParseException("MQTT envelope is missing 'message_id'.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Topic))
        {
            throw new IngestionParseException("MQTT envelope is missing 'topic'.");
        }

        if (envelope.Payload is null)
        {
            throw new IngestionParseException("MQTT envelope is missing 'payload'.");
        }

        if (envelope.Payload.Metrics is null || envelope.Payload.Metrics.Count == 0)
        {
            throw new IngestionParseException("MQTT payload does not contain any metrics.");
        }

        string deviceSerial = MqttTopicMapper.ExtractDeviceSerial(envelope.Topic);
        DateTimeOffset recordedAt = envelope.Payload.RecordedAt ?? envelope.Timestamp;

        return ValueTask.FromResult(new ParsedTelemetryBatch(
            MessageId: envelope.MessageId,
            DeviceExternalId: deviceSerial,
            RecordedAt: recordedAt,
            Metrics: envelope.Payload.Metrics,
            Source: MqttConstants.SourceName,
            Tags: envelope.Payload.Tags));
    }

    private static MqttIngestionEnvelope DeserializeEnvelope(ReadOnlySpan<byte> body)
    {
        try
        {
            MqttIngestionEnvelope? envelope = JsonSerializer.Deserialize(
                body, MqttJsonContext.Default.MqttIngestionEnvelope);
            if (envelope is null)
            {
                throw new IngestionParseException("MQTT envelope is empty.");
            }

            return envelope;
        }
        catch (JsonException ex)
        {
            throw new IngestionParseException("MQTT envelope is not valid JSON.", ex);
        }
    }
}
