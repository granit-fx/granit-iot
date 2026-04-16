using Granit.DataFiltering;
using Granit.Domain;
using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Internal;

internal sealed class DeviceLookupEfCore(
    IDbContextFactory<IoTDbContext> contextFactory,
    IDataFilter? dataFilter = null) : IDeviceLookup
{
    public async Task<DeviceLookupResult?> FindBySerialNumberAsync(
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        var sn = DeviceSerialNumber.Create(serialNumber);

        await using IoTDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        using IDisposable? tenantScope = dataFilter?.Disable<IMultiTenant>();

        return await db.Set<Device>()
            .AsNoTracking()
            .Where(d => d.SerialNumber == sn)
            .Select(d => new DeviceLookupResult(d.Id, d.TenantId))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
