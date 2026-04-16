using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Internal;

internal sealed class TelemetryEfCoreReader(
    IDbContextFactory<IoTDbContext> contextFactory,
    ICurrentTenant? currentTenant = null)
    : EfStoreBase<TelemetryPoint, IoTDbContext>(contextFactory, currentTenant), ITelemetryReader
{
    public async Task<IReadOnlyList<TelemetryPoint>> QueryAsync(
        Guid deviceId,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        int maxPoints = 500,
        CancellationToken cancellationToken = default)
    {
        return await ReadAsync(async db =>
            await Query(db)
                .Where(tp => tp.DeviceId == deviceId
                    && tp.RecordedAt >= rangeStart
                    && tp.RecordedAt <= rangeEnd)
                .OrderByDescending(tp => tp.RecordedAt)
                .Take(maxPoints)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<TelemetryPoint?> GetLatestAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await ReadAsync(async db =>
            await Query(db)
                .Where(tp => tp.DeviceId == deviceId)
                .OrderByDescending(tp => tp.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<TelemetryAggregate?> GetAggregateAsync(
        Guid deviceId,
        string metricName,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        TelemetryAggregation aggregation,
        CancellationToken cancellationToken = default)
    {
        return await ReadAsync(async db =>
        {
            // Push aggregation to the database.
            // For provider-agnostic LINQ, we filter telemetry points then compute in SQL.
            // JSONB-specific optimizations (e.g. metrics->>'key') are applied at the
            // PostgreSQL provider level via raw SQL if needed.
            IQueryable<TelemetryPoint> points = Query(db)
                .Where(tp => tp.DeviceId == deviceId
                    && tp.RecordedAt >= rangeStart
                    && tp.RecordedAt <= rangeEnd);

            long count = await points.LongCountAsync(cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                return null;
            }

            // For now, aggregate over the RecordedAt dimension.
            // Metric-specific aggregation (extracting values from the JSONB Metrics dict)
            // requires provider-specific SQL — the PostgreSQL variant can override this.
            double value = aggregation switch
            {
                TelemetryAggregation.Count => count,
                _ => count, // Metric-level Avg/Min/Max requires JSONB extraction — see PostgreSQL override
            };

            return new TelemetryAggregate(value, count, rangeStart, rangeEnd);
        }, cancellationToken).ConfigureAwait(false);
    }
}
