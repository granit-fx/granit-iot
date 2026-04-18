using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Raised when a device is provisioned and enters the <c>Provisioning</c> state.
/// In-process domain event — dispatched synchronously within the same transaction as
/// device creation. For cross-service propagation use <see cref="DeviceProvisionedEto"/>.
/// </summary>
/// <param name="DeviceId">Identifier of the newly provisioned device (UUID v7).</param>
/// <param name="SerialNumber">Manufacturer-supplied serial number, unique per tenant.</param>
/// <param name="TenantId">Tenant that owns the device, or <c>null</c> for host-owned devices.</param>
public sealed record DeviceProvisionedEvent(
    Guid DeviceId,
    string SerialNumber,
    Guid? TenantId) : IDomainEvent;
