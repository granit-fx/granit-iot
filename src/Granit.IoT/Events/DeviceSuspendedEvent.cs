using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Raised when a device is suspended (temporarily prevented from reporting telemetry).
/// Suspension is reversible via <see cref="DeviceReactivatedEvent"/>; permanent removal
/// uses <see cref="DeviceDecommissionedEvent"/> instead.
/// </summary>
/// <param name="DeviceId">Identifier of the suspended device.</param>
/// <param name="Reason">Operator-supplied suspension reason (free text, audit trail).</param>
/// <param name="TenantId">Tenant that owns the device, or <c>null</c> for host-owned devices.</param>
public sealed record DeviceSuspendedEvent(
    Guid DeviceId,
    string Reason,
    Guid? TenantId) : IDomainEvent;
