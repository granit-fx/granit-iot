using Granit.Settings.Definitions;

namespace Granit.IoT.Notifications.Internal;

/// <summary>
/// Registers per-tenant IoT settings (retention window, ingestion rate limit,
/// notification throttle, and the documentation entry for per-metric thresholds).
/// Auto-discovered by <c>GranitSettingsModule</c>.
/// </summary>
/// <remarks>
/// The runtime threshold lookup uses keys of the form
/// <c>IoT:Threshold:{metricName}</c> — those are open-ended and not registered
/// as individual definitions. The <c>IoT:Threshold</c> entry below carries the
/// documentation for the family.
/// </remarks>
internal sealed class IoTSettingDefinitionProvider : ISettingDefinitionProvider
{
    private const string TenantProvider = "T";
    private const string GlobalProvider = "G";

    public void Define(ISettingDefinitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Add(new SettingDefinition(IoTSettingNames.TelemetryRetentionDays)
        {
            DefaultValue = "90",
            IsVisibleToClients = true,
            DisplayName = "Telemetry retention (days)",
            Description = "Number of days a telemetry point is kept before the background purge job removes it.",
            Providers = { TenantProvider, GlobalProvider },
        });

        context.Add(new SettingDefinition(IoTSettingNames.IngestRateLimit)
        {
            DefaultValue = "1000",
            IsVisibleToClients = true,
            DisplayName = "Ingestion rate limit (requests / minute)",
            Description = "Maximum number of webhook ingestion requests accepted per tenant per minute.",
            Providers = { TenantProvider, GlobalProvider },
        });

        context.Add(new SettingDefinition(IoTSettingNames.NotificationThrottleMinutes)
        {
            DefaultValue = "15",
            IsVisibleToClients = true,
            DisplayName = "Notification throttle (minutes)",
            Description = "Minimum interval between threshold-alert notifications for the same (device, metric) pair. Prevents alert fatigue when a value oscillates around its threshold.",
            Providers = { TenantProvider, GlobalProvider },
        });

        // Documentation-only entry for the IoT:Threshold:{metric} family.
        // Runtime keys are read directly via ISettingProvider — see SettingsDeviceThresholdEvaluator.
        context.Add(new SettingDefinition("IoT:Threshold")
        {
            DefaultValue = null,
            IsVisibleToClients = false,
            DisplayName = "Per-metric thresholds (pattern)",
            Description = "Documentation for the IoT:Threshold:{metricName} setting family. Each runtime key holds the numeric threshold above which a TelemetryThresholdExceededEto is published (e.g. IoT:Threshold:temperature = 40).",
            Providers = { TenantProvider, GlobalProvider },
        });
    }
}
