using Granit.IoT.Domain;

namespace Granit.IoT.Abstractions;

/// <summary>Persists device changes (command side of CQRS).</summary>
public interface IDeviceWriter
{
    /// <summary>Persists a newly provisioned device.</summary>
    Task AddAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing device.</summary>
    Task UpdateAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a device.</summary>
    Task DeleteAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>Updates <c>LastHeartbeatAt</c> without loading the full aggregate (single UPDATE statement).</summary>
    Task UpdateHeartbeatAsync(Guid deviceId, DateTimeOffset heartbeatAt, CancellationToken cancellationToken = default);
}
