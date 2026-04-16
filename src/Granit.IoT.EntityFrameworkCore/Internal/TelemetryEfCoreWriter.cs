using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Internal;

internal sealed class TelemetryEfCoreWriter(
    IDbContextFactory<IoTDbContext> contextFactory,
    ICurrentTenant? currentTenant = null)
    : EfStoreBase<TelemetryPoint, IoTDbContext>(contextFactory, currentTenant), ITelemetryWriter
{
    public Task AppendAsync(TelemetryPoint point, CancellationToken cancellationToken = default) =>
        AddAsync(point, cancellationToken);

    public async Task AppendBatchAsync(IReadOnlyList<TelemetryPoint> points, CancellationToken cancellationToken = default)
    {
        if (points.Count == 0)
        {
            return;
        }

        await WriteAsync(async db =>
        {
            db.Set<TelemetryPoint>().AddRange(points);
            await Task.CompletedTask.ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }
}
