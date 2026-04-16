using Granit.Events;

namespace Granit.IoT.Events;

public sealed record DeviceDecommissionedEvent(
    Guid DeviceId,
    Guid? TenantId) : IDomainEvent;
