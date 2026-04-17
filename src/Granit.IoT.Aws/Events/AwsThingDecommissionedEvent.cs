using Granit.Events;
using Granit.IoT.Aws.Domain;

namespace Granit.IoT.Aws.Events;

/// <summary>
/// Raised when an <see cref="AwsThingBinding"/> has been removed from AWS
/// (Thing, certificate and secret deleted). Allows downstream Ring 3
/// integrations (Shadow polling, Jobs dispatcher) to release any cached state.
/// </summary>
public sealed record AwsThingDecommissionedEvent(
    Guid DeviceId,
    ThingName ThingName,
    Guid? TenantId) : IDomainEvent;
