using System.Buffers;
using System.Text.Json;
using Granit.IoT.Ingestion.Abstractions;

namespace Granit.IoT.Ingestion.Scaleway.Internal;

/// <summary>
/// Parses the Scaleway IoT Hub JSON envelope into a <see cref="ParsedTelemetryBatch"/>.
/// The envelope wraps the original MQTT payload as Base64; metric values are read from the
/// inner JSON object as <see cref="double"/>.
/// </summary>
internal sealed class ScalewayMessageParser(ScalewayTopicMapper topicMapper) : IInboundMessageParser
{
    public string SourceName => ScalewayConstants.SourceName;

    public ValueTask<ParsedTelemetryBatch> ParseAsync(
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        ScalewayEnvelope envelope = DeserializeEnvelope(body.Span);

        if (string.IsNullOrWhiteSpace(envelope.MessageId))
        {
            throw new IngestionParseException("Scaleway envelope is missing 'message_id'.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Topic))
        {
            throw new IngestionParseException("Scaleway envelope is missing 'topic'.");
        }

        if (string.IsNullOrEmpty(envelope.Payload))
        {
            throw new IngestionParseException("Scaleway envelope is missing 'payload'.");
        }

        string deviceSerial = topicMapper.ExtractDeviceSerial(envelope.Topic);
        Dictionary<string, double> metrics = DecodeMetrics(envelope.Payload);

        return ValueTask.FromResult(new ParsedTelemetryBatch(
            MessageId: envelope.MessageId,
            DeviceExternalId: deviceSerial,
            RecordedAt: envelope.Timestamp,
            Metrics: metrics,
            Source: ScalewayConstants.SourceName,
            Tags: null));
    }

    private static ScalewayEnvelope DeserializeEnvelope(ReadOnlySpan<byte> body)
    {
        try
        {
            ScalewayEnvelope? envelope = JsonSerializer.Deserialize(body, ScalewayJsonContext.Default.ScalewayEnvelope);
            if (envelope is null)
            {
                throw new IngestionParseException("Scaleway envelope is empty.");
            }

            return envelope;
        }
        catch (JsonException ex)
        {
            throw new IngestionParseException("Scaleway envelope is not valid JSON.", ex);
        }
    }

    private static Dictionary<string, double> DecodeMetrics(string base64Payload)
    {
        byte[]? rented = null;
        try
        {
            int maxBytes = ((base64Payload.Length * 3) + 3) / 4;
            rented = ArrayPool<byte>.Shared.Rent(maxBytes);
            if (!Convert.TryFromBase64String(base64Payload, rented, out int written))
            {
                throw new IngestionParseException("Scaleway payload is not valid Base64.");
            }

            try
            {
                Dictionary<string, double>? metrics = JsonSerializer.Deserialize(
                    new ReadOnlySpan<byte>(rented, 0, written),
                    ScalewayJsonContext.Default.DictionaryStringDouble);

                if (metrics is null || metrics.Count == 0)
                {
                    throw new IngestionParseException("Scaleway payload does not contain any metrics.");
                }

                return metrics;
            }
            catch (JsonException ex)
            {
                throw new IngestionParseException("Scaleway payload is not a valid JSON metric object.", ex);
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
