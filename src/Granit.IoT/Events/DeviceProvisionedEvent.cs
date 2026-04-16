using Granit.Events;

namespace Granit.IoT.Events;

public sealed record DeviceProvisionedEvent(
    Guid DeviceId,
    string SerialNumber,
    Guid? TenantId) : IDomainEvent;
