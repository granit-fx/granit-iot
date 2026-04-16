using Granit.Notifications;
using Granit.Notifications.Abstractions;

namespace Granit.IoT.Notifications.Internal;

/// <summary>
/// Registers the IoT notification definitions. Auto-discovered by
/// <c>GranitNotificationsModule</c> via DI.
/// </summary>
internal sealed class IoTNotificationDefinitionProvider : INotificationDefinitionProvider
{
    private const string GroupName = "IoT";

    public void Define(INotificationDefinitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Add(new NotificationDefinition(IoTTelemetryThresholdAlertNotificationType.Instance.Name)
        {
            GroupName = GroupName,
            DisplayName = "Telemetry threshold exceeded",
            Description = "Alert raised when a device metric breaches its configured threshold (e.g. temperature, vibration, pressure).",
            DefaultSeverity = NotificationSeverity.Warning,
            DefaultChannels = [NotificationChannels.Email, NotificationChannels.Push],
            AllowUserOptOut = false,
        });

        context.Add(new NotificationDefinition(IoTDeviceOfflineNotificationType.Instance.Name)
        {
            GroupName = GroupName,
            DisplayName = "Device offline",
            Description = "Critical alert raised when a device stops emitting heartbeats and is considered offline.",
            DefaultSeverity = NotificationSeverity.Fatal,
            DefaultChannels = [NotificationChannels.Email, NotificationChannels.Push, NotificationChannels.Sms],
            AllowUserOptOut = false,
        });
    }
}
