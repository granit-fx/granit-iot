using System.Text.Json;
using Granit.IoT.Ingestion.Abstractions;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Parses the IoT Rule JSON envelope that the Direct and API Gateway paths
/// both produce. Shared implementation with a different <see cref="SourceName"/>
/// per registered instance (see
/// <see cref="Extensions.IoTIngestionAwsServiceCollectionExtensions"/>).
/// </summary>
internal sealed class AwsIoTRulePayloadParser(string sourceName) : IInboundMessageParser
{
    public string SourceName { get; } = sourceName;

    public ValueTask<ParsedTelemetryBatch> ParseAsync(
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(Parse(body, SourceName));

    internal static ParsedTelemetryBatch Parse(ReadOnlyMemory<byte> body, string source)
    {
        AwsIoTRuleEnvelope envelope = DeserializeEnvelope(body.Span);

        if (string.IsNullOrWhiteSpace(envelope.MessageId))
        {
            throw new IngestionParseException("AWS IoT Rule envelope is missing 'messageId'.");
        }

        if (string.IsNullOrWhiteSpace(envelope.DeviceId))
        {
            throw new IngestionParseException("AWS IoT Rule envelope is missing 'deviceId'.");
        }

        if (envelope.Metrics is null || envelope.Metrics.Count == 0)
        {
            throw new IngestionParseException(
                "AWS IoT Rule envelope must contain a non-empty 'metrics' object.");
        }

        DateTimeOffset recordedAt = ExtractTimestamp(envelope.Timestamp);

        return new ParsedTelemetryBatch(
            MessageId: envelope.MessageId,
            DeviceExternalId: envelope.DeviceId,
            RecordedAt: recordedAt,
            Metrics: envelope.Metrics,
            Source: source,
            Tags: null);
    }

    private static AwsIoTRuleEnvelope DeserializeEnvelope(ReadOnlySpan<byte> body)
    {
        try
        {
            AwsIoTRuleEnvelope? envelope = JsonSerializer.Deserialize<AwsIoTRuleEnvelope>(body);
            if (envelope is null)
            {
                throw new IngestionParseException("AWS IoT Rule envelope is empty.");
            }

            return envelope;
        }
        catch (JsonException ex)
        {
            throw new IngestionParseException("AWS IoT Rule envelope is not valid JSON.", ex);
        }
    }

    /// <summary>
    /// Accepts both an ISO-8601 string (<c>"2026-04-17T12:00:00.000Z"</c>)
    /// and Unix-epoch milliseconds as a number (what
    /// <c>timestamp()</c> returns in an IoT Rule SELECT).
    /// </summary>
    private static DateTimeOffset ExtractTimestamp(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => ParseIsoDate(element.GetString()),
            JsonValueKind.Number => DateTimeOffset.FromUnixTimeMilliseconds(element.GetInt64()),
            _ => throw new IngestionParseException(
                "AWS IoT Rule envelope 'timestamp' must be an ISO-8601 string or Unix milliseconds."),
        };
    }

    private static DateTimeOffset ParseIsoDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset parsed))
        {
            throw new IngestionParseException(
                $"AWS IoT Rule envelope 'timestamp' string '{value}' is not a valid ISO-8601 date.");
        }
        return parsed;
    }
}
