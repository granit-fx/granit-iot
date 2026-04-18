using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Raised when a previously suspended device is returned to the <c>Active</c> state.
/// </summary>
/// <param name="DeviceId">Identifier of the reactivated device.</param>
/// <param name="SerialNumber">Manufacturer-supplied serial number, unique per tenant.</param>
/// <param name="TenantId">Tenant that owns the device, or <c>null</c> for host-owned devices.</param>
public sealed record DeviceReactivatedEvent(
    Guid DeviceId,
    string SerialNumber,
    Guid? TenantId) : IDomainEvent;
