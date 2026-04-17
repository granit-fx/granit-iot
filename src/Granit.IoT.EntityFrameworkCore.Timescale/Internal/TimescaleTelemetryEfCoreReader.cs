using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.IoT.EntityFrameworkCore.Postgres.Internal;
using Granit.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Timescale.Internal;

/// <summary>
/// TimescaleDB-specific telemetry reader. Routes <c>GetAggregateAsync</c> to the
/// right data source based on the time-window size:
/// <list type="bullet">
///   <item><description>≥ 1 day → <c>iot_telemetry_daily</c> continuous aggregate</description></item>
///   <item><description>≥ 1 hour → <c>iot_telemetry_hourly</c> continuous aggregate</description></item>
///   <item><description>sub-hourly → raw hypertable via the base JSONB path (inherited)</description></item>
/// </list>
/// Continuous aggregates are pre-materialized so dashboard queries over days of
/// telemetry complete in single-digit milliseconds even on billion-row tables.
/// </summary>
internal sealed class TimescaleTelemetryEfCoreReader(
    IDbContextFactory<IoTDbContext> contextFactory,
    ICurrentTenant? currentTenant = null)
    : PostgresTelemetryEfCoreReader(contextFactory, currentTenant)
{
    /// <summary>Windows this wide or wider are served from the daily continuous aggregate.</summary>
    internal static readonly TimeSpan DailyAggregateMinWindow = TimeSpan.FromDays(1);

    /// <summary>Windows this wide or wider (and under daily threshold) are served from the hourly continuous aggregate.</summary>
    internal static readonly TimeSpan HourlyAggregateMinWindow = TimeSpan.FromHours(1);

    public override Task<TelemetryAggregate?> GetAggregateAsync(
        Guid deviceId,
        string metricName,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        TelemetryAggregation aggregation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(metricName);

        string? aggregateView = SelectAggregateView(rangeEnd - rangeStart);

        if (aggregateView is null)
        {
            return base.GetAggregateAsync(deviceId, metricName, rangeStart, rangeEnd, aggregation, cancellationToken);
        }

        // Continuous aggregates already store avg/min/max/count per (bucket, device, metric).
        // For Avg we take the count-weighted average across buckets; Min/Max bubble up; Count sums.
        string selectExpr = aggregation switch
        {
            TelemetryAggregation.Avg => "COALESCE(SUM(avg_value * count) / NULLIF(SUM(count), 0), 0)",
            TelemetryAggregation.Min => "MIN(min_value)",
            TelemetryAggregation.Max => "MAX(max_value)",
            TelemetryAggregation.Count => "SUM(count)::double precision",
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation), aggregation, null),
        };

        return ExecuteAggregateSqlAsync(
            tenantPredicate =>
                $"SELECT {selectExpr} AS \"Value\", COALESCE(SUM(count), 0) AS \"Count\" " +
                $"FROM \"{aggregateView}\" " +
                "WHERE \"DeviceId\" = @deviceId " +
                "  AND \"MetricName\" = @metric " +
                "  AND bucket >= @rangeStart " +
                "  AND bucket < @rangeEnd" +
                tenantPredicate,
            deviceId,
            metricName,
            rangeStart,
            rangeEnd,
            cancellationToken);
    }

    /// <summary>
    /// Returns the unqualified name of the continuous aggregate to use for a given
    /// window, or <c>null</c> when the window is narrower than one hour (the
    /// caller should fall back to the raw hypertable for sub-hourly precision).
    /// </summary>
    internal static string? SelectAggregateView(TimeSpan window)
    {
        if (window >= DailyAggregateMinWindow)
        {
            return TimescaleSqlBuilder.DailyAggregateView;
        }

        if (window >= HourlyAggregateMinWindow)
        {
            return TimescaleSqlBuilder.HourlyAggregateView;
        }

        return null;
    }
}
