using Granit.IoT.Domain;

namespace Granit.IoT.Abstractions;

/// <summary>Reads device data (query side of CQRS).</summary>
public interface IDeviceReader
{
    /// <summary>Returns a device by ID, or <c>null</c> if not found (tenant-scoped).</summary>
    Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a device by serial number within the current tenant.</summary>
    Task<Device?> FindBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);

    /// <summary>Returns devices for the current tenant, optionally filtered by status.</summary>
    Task<IReadOnlyList<Device>> ListAsync(
        DeviceStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the total count of devices for the current tenant, optionally filtered by status.</summary>
    Task<int> CountAsync(DeviceStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a device with the given serial number exists in the current tenant.</summary>
    Task<bool> ExistsAsync(string serialNumber, CancellationToken cancellationToken = default);
}
