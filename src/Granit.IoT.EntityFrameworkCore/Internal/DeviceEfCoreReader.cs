using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Internal;

internal sealed class DeviceEfCoreReader(
    IDbContextFactory<IoTDbContext> contextFactory,
    ICurrentTenant? currentTenant = null)
    : EfStoreBase<Device, IoTDbContext>(contextFactory, currentTenant), IDeviceReader
{
    public Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        FindByIdAsync(id, cancellationToken);

    public async Task<Device?> FindBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var sn = DeviceSerialNumber.Create(serialNumber);
        return await FirstOrDefaultAsync(d => d.SerialNumber == sn, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Device>> ListAsync(
        DeviceStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await ReadAsync(async db =>
        {
            IQueryable<Device> query = Query(db);

            if (status.HasValue)
            {
                query = query.Where(d => d.Status == status.Value);
            }

            return await query
                .OrderBy(d => d.Label)
                .ThenBy(d => d.SerialNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task<int> CountAsync(DeviceStatus? status = null, CancellationToken cancellationToken = default) =>
        status.HasValue
            ? CountAsync(d => d.Status == status.Value, cancellationToken)
            : CountAsync(predicate: null, cancellationToken);

    public async Task<bool> ExistsAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var sn = DeviceSerialNumber.Create(serialNumber);
        return await AnyAsync(d => d.SerialNumber == sn, cancellationToken).ConfigureAwait(false);
    }
}
