using Granit.Notifications;

namespace Granit.IoT.Notifications;

/// <summary>
/// Sent when a device misses heartbeats long enough to be considered offline.
/// Delivered to the followers of the affected <c>Device</c> entity through Email,
/// Push, and SMS so an on-call technician can react quickly.
/// </summary>
public sealed class IoTDeviceOfflineNotificationType
    : NotificationType<IoTDeviceOfflineData>
{
    /// <summary>Singleton instance.</summary>
    public static readonly IoTDeviceOfflineNotificationType Instance = new();

    /// <inheritdoc/>
    public override string Name => "IoT.DeviceOffline";

    /// <inheritdoc/>
    public override NotificationSeverity DefaultSeverity => NotificationSeverity.Fatal;

    /// <inheritdoc/>
    public override IReadOnlyList<string> DefaultChannels { get; } =
        [NotificationChannels.Email, NotificationChannels.Push, NotificationChannels.Sms];
}

/// <summary>Payload for <see cref="IoTDeviceOfflineNotificationType"/>.</summary>
/// <param name="DeviceId">Internal identifier of the offline device.</param>
/// <param name="LastSeenAt">Timestamp of the most recent heartbeat received from the device.</param>
public sealed record IoTDeviceOfflineData(
    Guid DeviceId,
    DateTimeOffset LastSeenAt);
