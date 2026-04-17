using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Granit.IoT.Aws.Jobs.Diagnostics;

/// <summary>
/// OpenTelemetry counters for the AWS IoT Jobs dispatcher and poller.
/// Tagged with <c>tenant_id</c> (coalesced to <c>"global"</c>) and the
/// command's <c>operation</c> so dashboards can split by activity type.
/// </summary>
public sealed class AwsJobsMetrics
{
    public const string MeterName = "Granit.IoT.Aws.Jobs";

    private readonly Counter<long> _dispatched;
    private readonly Counter<long> _idempotentReuse;
    private readonly Counter<long> _completed;
    private readonly Counter<long> _failed;
    private readonly Counter<long> _dispatchErrors;

    public AwsJobsMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        Meter meter = meterFactory.Create(MeterName);

        _dispatched = meter.CreateCounter<long>(
            "granit.iot.aws.jobs.dispatched",
            unit: "{job}",
            description: "AWS IoT Jobs created by the dispatcher.");
        _idempotentReuse = meter.CreateCounter<long>(
            "granit.iot.aws.jobs.idempotent_reuse",
            unit: "{job}",
            description: "Re-dispatches that hit JobAlreadyExists and reused the existing job.");
        _completed = meter.CreateCounter<long>(
            "granit.iot.aws.jobs.completed",
            unit: "{execution}",
            description: "Job executions surfaced as DeviceCommandCompletedEvent.");
        _failed = meter.CreateCounter<long>(
            "granit.iot.aws.jobs.failed",
            unit: "{execution}",
            description: "Job executions surfaced as DeviceCommandFailedEvent.");
        _dispatchErrors = meter.CreateCounter<long>(
            "granit.iot.aws.jobs.dispatch_errors",
            unit: "{error}",
            description: "Dispatch failures unrelated to JobAlreadyExists (network, throttling, …).");
    }

    public void RecordDispatched(Guid? tenantId, string operation) =>
        _dispatched.Add(1, BuildTags(tenantId, operation));

    public void RecordIdempotentReuse(Guid? tenantId, string operation) =>
        _idempotentReuse.Add(1, BuildTags(tenantId, operation));

    public void RecordCompleted(Guid? tenantId) =>
        _completed.Add(1, BuildTags(tenantId, operation: null));

    public void RecordFailed(Guid? tenantId) =>
        _failed.Add(1, BuildTags(tenantId, operation: null));

    public void RecordDispatchError(Guid? tenantId, string operation) =>
        _dispatchErrors.Add(1, BuildTags(tenantId, operation));

    private static TagList BuildTags(Guid? tenantId, string? operation)
    {
        var tags = new TagList { { "tenant_id", tenantId?.ToString("N") ?? "global" } };
        if (!string.IsNullOrEmpty(operation))
        {
            tags.Add("operation", operation);
        }
        return tags;
    }
}
