using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Raised when a device transitions from <c>Provisioning</c> to <c>Active</c> —
/// typically after the first valid heartbeat or an explicit activation call.
/// </summary>
/// <param name="DeviceId">Identifier of the activated device.</param>
/// <param name="SerialNumber">Manufacturer-supplied serial number, unique per tenant.</param>
/// <param name="TenantId">Tenant that owns the device, or <c>null</c> for host-owned devices.</param>
public sealed record DeviceActivatedEvent(
    Guid DeviceId,
    string SerialNumber,
    Guid? TenantId) : IDomainEvent;
