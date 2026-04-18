namespace Granit.IoT.Endpoints.Dtos;

/// <summary>
/// A single telemetry reading as returned by history endpoints. Immutable —
/// telemetry rows are append-only per CLAUDE.md §Key design decisions.
/// </summary>
/// <param name="Id">Telemetry point identifier (UUID v7, time-ordered).</param>
/// <param name="DeviceId">Device that reported the reading.</param>
/// <param name="RecordedAt">Timestamp the device claims for the reading (device clock, not ingestion time).</param>
/// <param name="Metrics">Flat key→double map of measurements for this point (e.g. <c>{"temperature": 21.4, "humidity": 55.2}</c>).</param>
/// <param name="Source">Ingestion source name (e.g. <c>"scaleway"</c>, <c>"aws-sns"</c>, <c>"mqtt"</c>). <c>null</c> for legacy rows.</param>
public sealed record TelemetryPointResponse(
    Guid Id,
    Guid DeviceId,
    DateTimeOffset RecordedAt,
    IReadOnlyDictionary<string, double> Metrics,
    string? Source);
