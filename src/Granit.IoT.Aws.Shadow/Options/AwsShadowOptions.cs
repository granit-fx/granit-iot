using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Aws.Shadow.Options;

/// <summary>
/// Configuration for the AWS Device Shadow bridge.
/// </summary>
public sealed class AwsShadowOptions
{
    /// <summary>Configuration binding section: <c>IoT:Aws:Shadow</c>.</summary>
    public const string SectionName = "IoT:Aws:Shadow";

    /// <summary>
    /// How often the polling service walks active bindings looking for a
    /// desired/reported delta. Default 30s. Larger values mean cloud-issued
    /// commands take longer to propagate; smaller values increase API cost
    /// (Device Shadow data plane is rate-limited per account).
    /// </summary>
    [Range(5, 600)]
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of bindings inspected in a single polling tick. Acts
    /// as a sliding window so a fleet of N devices is fully scanned every
    /// <c>ceil(N / PollBatchSize) * PollIntervalSeconds</c> seconds.
    /// </summary>
    [Range(1, 1000)]
    public int PollBatchSize { get; set; } = 100;

    /// <summary>
    /// When <c>true</c>, the bridge automatically pushes a JSON
    /// <c>{"status":"…"}</c> reported document on every device lifecycle
    /// transition (Activated/Suspended/Reactivated/Decommissioned). Turn off
    /// when consumers prefer to manage the shadow document themselves
    /// through their own Wolverine handlers.
    /// </summary>
    public bool AutoPushLifecycleStatus { get; set; } = true;
}
