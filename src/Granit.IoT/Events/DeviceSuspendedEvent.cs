using Granit.Events;

namespace Granit.IoT.Events;

public sealed record DeviceSuspendedEvent(
    Guid DeviceId,
    string Reason,
    Guid? TenantId) : IDomainEvent;
