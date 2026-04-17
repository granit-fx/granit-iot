using Granit.Events;

namespace Granit.IoT.Aws.Jobs.Events;

/// <summary>
/// Raised by the polling service when an AWS IoT Job execution finishes
/// with the <c>SUCCEEDED</c> status. The originating command is identified
/// by <see cref="CorrelationId"/>.
/// </summary>
public sealed record DeviceCommandCompletedEvent(
    Guid CorrelationId,
    string JobId,
    string ThingName,
    DateTimeOffset CompletedAt,
    Guid? TenantId) : IDomainEvent;
