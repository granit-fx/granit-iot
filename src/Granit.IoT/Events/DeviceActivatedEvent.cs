using Granit.Events;

namespace Granit.IoT.Events;

public sealed record DeviceActivatedEvent(
    Guid DeviceId,
    string SerialNumber,
    Guid? TenantId) : IDomainEvent;
