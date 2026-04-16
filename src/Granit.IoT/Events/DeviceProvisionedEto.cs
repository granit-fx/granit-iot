using Granit.Events;

namespace Granit.IoT.Events;

public sealed record DeviceProvisionedEto(
    Guid DeviceId,
    string SerialNumber,
    string HardwareModel,
    Guid? TenantId) : IIntegrationEvent;
