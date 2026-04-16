namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Provider-neutral representation of a single inbound telemetry payload. Produced by
/// <see cref="IInboundMessageParser"/> and consumed by the ingestion pipeline.
/// </summary>
/// <param name="MessageId">Transport-level message identifier (used for deduplication).</param>
/// <param name="DeviceExternalId">Device serial number as exposed by the IoT hub.</param>
/// <param name="RecordedAt">Timestamp at which the device emitted the payload.</param>
/// <param name="Metrics">Metric name/value pairs extracted from the payload.</param>
/// <param name="Source">Provider source identifier (e.g. <c>"scaleway"</c>).</param>
/// <param name="Tags">Optional device-supplied tags forwarded to telemetry storage.</param>
public sealed record ParsedTelemetryBatch(
    string MessageId,
    string DeviceExternalId,
    DateTimeOffset RecordedAt,
    IReadOnlyDictionary<string, double> Metrics,
    string Source,
    IReadOnlyDictionary<string, string>? Tags = null);
