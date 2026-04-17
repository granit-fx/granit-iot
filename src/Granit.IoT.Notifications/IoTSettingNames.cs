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

    /// <summary>
    /// Minutes since a device's last heartbeat after which it is considered offline.
    /// Set to <c>0</c> to disable the heartbeat check for the tenant.
    /// </summary>
    public const string HeartbeatTimeoutMinutes = "IoT:HeartbeatTimeoutMinutes";

    /// <summary>
    /// How long the heartbeat job remembers a device that has already been
    /// reported offline, preventing alert spam on flaky links (LoRa / NB-IoT).
    /// </summary>
    public const string HeartbeatOfflineNotificationCacheMinutes = "IoT:HeartbeatOfflineNotificationCacheMinutes";
}
