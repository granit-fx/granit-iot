using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Raised when a device is permanently decommissioned (removed from active service).
/// Terminal state — a decommissioned device cannot be reactivated. Downstream handlers
/// typically revoke credentials, archive telemetry, and release the serial number for reuse.
/// </summary>
/// <param name="DeviceId">Identifier of the decommissioned device.</param>
/// <param name="TenantId">Tenant that owned the device, or <c>null</c> for host-owned devices.</param>
public sealed record DeviceDecommissionedEvent(
    Guid DeviceId,
    Guid? TenantId) : IDomainEvent;
