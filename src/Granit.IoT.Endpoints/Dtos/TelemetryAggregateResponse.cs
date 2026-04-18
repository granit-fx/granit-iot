namespace Granit.IoT.Endpoints.Dtos;

/// <summary>
/// Aggregate result returned by telemetry roll-up endpoints (min / max / avg / sum / count).
/// One row per (metric, time-bucket) pair.
/// </summary>
/// <param name="Value">Aggregated numeric value for the window. Semantics depend on <see cref="Aggregation"/>.</param>
/// <param name="Count">Number of raw telemetry points contributing to <see cref="Value"/>.</param>
/// <param name="MetricName">Name of the metric that was aggregated (e.g. <c>"temperature"</c>).</param>
/// <param name="Aggregation">Aggregation function applied: <c>Min</c>, <c>Max</c>, <c>Avg</c>, <c>Sum</c>, <c>Count</c>.</param>
/// <param name="RangeStart">Inclusive lower bound of the aggregation window (UTC).</param>
/// <param name="RangeEnd">Inclusive upper bound of the aggregation window (UTC).</param>
public sealed record TelemetryAggregateResponse(
    double Value,
    long Count,
    string MetricName,
    string Aggregation,
    DateTimeOffset RangeStart,
    DateTimeOffset RangeEnd);
