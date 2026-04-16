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
    Avg,
    Min,
    Max,
    Count,
}

/// <summary>Result of a server-side telemetry aggregation.</summary>
public sealed record TelemetryAggregate(
    double Value,
    long Count,
    DateTimeOffset RangeStart,
    DateTimeOffset RangeEnd);
