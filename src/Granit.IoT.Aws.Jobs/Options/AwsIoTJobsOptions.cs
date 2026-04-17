using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Aws.Jobs.Options;

/// <summary>
/// Configuration for the AWS IoT Jobs dispatcher and the matching status
/// poller.
/// </summary>
public sealed class AwsIoTJobsOptions
{
    /// <summary>Configuration binding section: <c>IoT:Aws:Jobs</c>.</summary>
    public const string SectionName = "IoT:Aws:Jobs";

    /// <summary>
    /// AWS Job id prefix. Defaults to <c>granit</c> so jobs surface as
    /// <c>granit-{correlationId}</c>. Useful when several Granit
    /// applications share a single AWS account.
    /// </summary>
    public string JobIdPrefix { get; set; } = "granit";

    /// <summary>
    /// How long the dispatcher tracks correlationId → jobId after dispatch.
    /// The polling service will stop watching jobs whose tracking entry has
    /// expired — leftover unfinished executions surface in CloudWatch
    /// instead of producing late completion events.
    /// </summary>
    [Range(1, 720)]
    public int JobTrackingTtlHours { get; set; } = 72;

    /// <summary>
    /// How often the polling service inspects tracked jobs. Default 5 min
    /// matches the original story's `[RecurringJob("*/5 * * * *")]` cadence.
    /// </summary>
    [Range(15, 3600)]
    public int StatusPollIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum number of in-flight jobs inspected per poll tick. Acts as a
    /// rate-limit shock absorber against AWS IoT Jobs control-plane quotas.
    /// </summary>
    [Range(1, 1000)]
    public int StatusPollBatchSize { get; set; } = 100;
}
