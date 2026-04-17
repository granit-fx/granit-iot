using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Aws.FleetProvisioning.Options;

/// <summary>
/// Configuration for the JITP endpoints + claim-certificate rotation check.
/// </summary>
public sealed class FleetProvisioningOptions
{
    /// <summary>Configuration binding section: <c>IoT:Aws:FleetProvisioning</c>.</summary>
    public const string SectionName = "IoT:Aws:FleetProvisioning";

    /// <summary>
    /// How many days before a claim certificate expires the rotation check
    /// starts publishing <c>ClaimCertificateExpiringEvent</c>. Default 30 —
    /// long enough that a missed alert in week one still leaves three weeks
    /// of follow-up windows, short enough to avoid year-round noise.
    /// </summary>
    [Range(1, 180)]
    public int ExpiryWarningWindowDays { get; set; } = 30;

    /// <summary>
    /// How often the rotation check sweeps active bindings. Default 24h —
    /// the warning window is measured in days so a daily cadence is
    /// sufficient and aligns with the original story's daily cron.
    /// </summary>
    [Range(1, 168)]
    public int RotationCheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// Maximum number of bindings inspected in a single rotation tick. The
    /// expiry check is in-memory after the bindings are loaded, so the
    /// limit acts as a memory bound rather than an AWS-side rate limit.
    /// </summary>
    [Range(1, 10000)]
    public int RotationCheckBatchSize { get; set; } = 1000;
}
