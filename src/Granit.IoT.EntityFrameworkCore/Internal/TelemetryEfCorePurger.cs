using Granit.IoT.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Internal;

internal sealed class TelemetryEfCorePurger(IDbContextFactory<IoTDbContext> contextFactory)
    : ITelemetryPurger
{
    public async Task<long> PurgeOlderThanAsync(
        IReadOnlyCollection<Guid?> tenantIds,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantIds);
        if (tenantIds.Count == 0)
        {
            return 0;
        }

        await using IoTDbContext db = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.TelemetryPoints
            .IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.TenantId) && t.RecordedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
