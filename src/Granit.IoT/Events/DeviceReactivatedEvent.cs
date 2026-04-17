using Granit.Events;

namespace Granit.IoT.Events;

public sealed record DeviceReactivatedEvent(
    Guid DeviceId,
    string SerialNumber,
    Guid? TenantId) : IDomainEvent;
