using Granit.Events;

namespace Granit.IoT.Aws.Shadow.Events;

/// <summary>
/// Raised by the polling service when AWS IoT's Device Shadow surfaces a
/// non-empty <c>delta</c> for a tracked binding. The PR #6 IoT Jobs
/// dispatcher (story #49) consumes this event and turns each delta key
/// into a device-targeted command. The cloud-agnostic <c>Device</c>
/// aggregate never sees this event — the desired state is interpreted
/// AWS-side only.
/// </summary>
public sealed record DeviceDesiredStateChangedEvent(
    Guid DeviceId,
    string ThingName,
    IReadOnlyDictionary<string, object?> Delta,
    long ShadowVersion,
    Guid? TenantId) : IDomainEvent;
