using Granit.IoT.Domain;

namespace Granit.IoT.Abstractions;

/// <summary>Reads telemetry data (query side of CQRS).</summary>
public interface ITelemetryReader
{
    /// <summary>Returns telemetry points for a device within a time range, ordered by <c>RecordedAt</c> descending.</summary>
    Task<IReadOnlyList<TelemetryPoint>> QueryAsync(
        Guid deviceId,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        int maxPoints = 500,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent telemetry point for a device.</summary>
    Task<TelemetryPoint?> GetLatestAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>Computes an aggregate (avg, min, max, count) for a specific metric over a time range, pushed to the database.</summary>
    Task<TelemetryAggregate?> GetAggregateAsync(
        Guid deviceId,
        string metricName,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        TelemetryAggregation aggregation,
        CancellationToken cancellationToken = default);
}

/// <summary>The aggregation function to apply on a metric.</summary>
public enum TelemetryAggregation
{
    /// <summary>Arithmetic mean over the window.</summary>
    Avg,

    /// <summary>Minimum observed value.</summary>
    Min,

    /// <summary>Maximum observed value.</summary>
    Max,

    /// <summary>Number of points that contributed to the aggregation.</summary>
    Count,
}

/// <summary>Result of a server-side telemetry aggregation.</summary>
/// <param name="Value">Aggregated numeric value. Semantics depend on the <see cref="TelemetryAggregation"/> requested.</param>
/// <param name="Count">Number of raw telemetry points contributing to <see cref="Value"/>.</param>
/// <param name="RangeStart">Inclusive lower bound of the aggregation window (UTC).</param>
/// <param name="RangeEnd">Inclusive upper bound of the aggregation window (UTC).</param>
public sealed record TelemetryAggregate(
    double Value,
    long Count,
    DateTimeOffset RangeStart,
    DateTimeOffset RangeEnd);
