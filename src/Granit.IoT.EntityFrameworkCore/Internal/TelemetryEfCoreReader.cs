using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Internal;

internal class TelemetryEfCoreReader(
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

    public virtual async Task<TelemetryAggregate?> GetAggregateAsync(
        Guid deviceId,
        string metricName,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        TelemetryAggregation aggregation,
        CancellationToken cancellationToken = default)
    {
        // Provider-agnostic path: only Count is portable. Metric-level Avg/Min/Max
        // requires JSONB extraction pushed to the database — implemented by the
        // PostgreSQL override (PostgresTelemetryEfCoreReader). Raising here prevents
        // silent data corruption when a non-PostgreSQL provider is used.
        if (aggregation is not TelemetryAggregation.Count)
        {
            throw new NotSupportedException(
                $"Aggregation '{aggregation}' on metric '{metricName}' requires a provider-specific reader. " +
                "Register Granit.IoT.EntityFrameworkCore.Postgres (AddGranitIoTPostgres) to enable Avg/Min/Max.");
        }

        return await ReadAsync(async db =>
        {
            long count = await Query(db)
                .Where(tp => tp.DeviceId == deviceId
                    && tp.RecordedAt >= rangeStart
                    && tp.RecordedAt <= rangeEnd)
                .LongCountAsync(cancellationToken).ConfigureAwait(false);

            return count == 0
                ? null
                : new TelemetryAggregate(count, count, rangeStart, rangeEnd);
        }, cancellationToken).ConfigureAwait(false);
    }
}
