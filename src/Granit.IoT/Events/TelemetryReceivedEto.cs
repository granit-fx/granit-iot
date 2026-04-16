using Granit.Events;

namespace Granit.IoT.Events;

public sealed record TelemetryReceivedEto(
    Guid DeviceId,
    Guid TelemetryPointId,
    DateTimeOffset RecordedAt,
    Guid? TenantId) : IIntegrationEvent;
