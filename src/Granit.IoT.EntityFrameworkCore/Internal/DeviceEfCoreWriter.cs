using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Internal;

internal sealed class DeviceEfCoreWriter(
    IDbContextFactory<IoTDbContext> contextFactory,
    ICurrentTenant? currentTenant = null)
    : EfStoreBase<Device, IoTDbContext>(contextFactory, currentTenant), IDeviceWriter
{
    public new Task AddAsync(Device device, CancellationToken cancellationToken = default) =>
        base.AddAsync(device, cancellationToken);

    public new Task UpdateAsync(Device device, CancellationToken cancellationToken = default) =>
        base.UpdateAsync(device, cancellationToken);

    public new Task DeleteAsync(Device device, CancellationToken cancellationToken = default) =>
        base.DeleteAsync(device, cancellationToken);

    public async Task UpdateHeartbeatAsync(Guid deviceId, DateTimeOffset heartbeatAt, CancellationToken cancellationToken = default)
    {
        await WriteAsync(async db =>
        {
            await db.Devices
                .Where(d => d.Id == deviceId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(d => d.LastHeartbeatAt, heartbeatAt),
                    cancellationToken)
                .ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }
}
