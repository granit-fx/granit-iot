namespace Granit.IoT.Notifications;

/// <summary>
/// Setting keys consumed by the IoT module family. Auto-discovered by
/// <c>Granit.Settings</c> via <see cref="Internal.IoTSettingDefinitionProvider"/>.
/// </summary>
public static class IoTSettingNames
{
    /// <summary>Number of days after which telemetry points become eligible for purge.</summary>
    public const string TelemetryRetentionDays = "IoT:TelemetryRetentionDays";

    /// <summary>Per-tenant ingestion rate limit (requests / minute).</summary>
    public const string IngestRateLimit = "IoT:IngestRateLimit";

    /// <summary>Minimum interval (minutes) between alerts for the same (device, metric) pair.</summary>
    public const string NotificationThrottleMinutes = "IoT:NotificationThrottleMinutes";

    /// <summary>
    /// Pattern key for per-metric thresholds. Runtime keys are
    /// <c>IoT:Threshold:{metricName}</c> (e.g. <c>IoT:Threshold:temperature</c>).
    /// </summary>
    public const string ThresholdPrefix = "IoT:Threshold:";
}
