using Granit.Events;
using Granit.IoT.Aws.Domain;

namespace Granit.IoT.Aws.Events;

/// <summary>
/// Raised when an <see cref="AwsThingBinding"/> reaches
/// <see cref="AwsThingProvisioningStatus.Active"/> — the device is fully
/// registered with AWS IoT and may connect with its certificate.
/// </summary>
public sealed record AwsThingProvisionedEvent(
    Guid DeviceId,
    ThingName ThingName,
    string ThingArn,
    Guid? TenantId) : IDomainEvent;
