using Granit.Events;

namespace Granit.IoT.Aws.Jobs.Events;

/// <summary>
/// Raised by the polling service when an AWS IoT Job execution finishes
/// with the <c>FAILED</c>, <c>REJECTED</c>, <c>TIMED_OUT</c>, <c>CANCELED</c>
/// or <c>REMOVED</c> status. <see cref="Reason"/> carries the AWS-side
/// detailed status when one was provided.
/// </summary>
public sealed record DeviceCommandFailedEvent(
    Guid CorrelationId,
    string JobId,
    string ThingName,
    string Status,
    string? Reason,
    DateTimeOffset FailedAt,
    Guid? TenantId) : IDomainEvent;
