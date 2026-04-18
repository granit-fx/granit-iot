using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Integration event published through the Wolverine outbox when a device is provisioned.
/// Flat serializable snapshot — no navigations, no services — stable across service
/// boundaries and durable across retries.
/// </summary>
/// <param name="DeviceId">Identifier of the newly provisioned device (UUID v7).</param>
/// <param name="SerialNumber">Manufacturer-supplied serial number, unique per tenant.</param>
/// <param name="HardwareModel">Hardware model identifier at provisioning time.</param>
/// <param name="TenantId">Tenant that owns the device, or <c>null</c> for host-owned devices.</param>
public sealed record DeviceProvisionedEto(
    Guid DeviceId,
    string SerialNumber,
    string HardwareModel,
    Guid? TenantId) : IIntegrationEvent;
