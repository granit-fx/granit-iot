using Granit.Domain;
using Granit.IoT.Diagnostics;
using Granit.IoT.Events;
using Granit.Notifications.Abstractions;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Notifications.Handlers;

/// <summary>
/// Wolverine handler that turns each <see cref="DeviceOfflineDetectedEto"/>
/// (published by <c>DeviceHeartbeatTimeoutJob</c>) into a
/// <c>IoT.DeviceOffline</c> notification delivered to the followers of the
/// originating device.
/// </summary>
public static partial class DeviceOfflineNotificationHandler
{
    private const string DeviceEntityType = "Device";

    /// <summary>
    /// Handles <see cref="DeviceOfflineDetectedEto"/> — publishes a
    /// <c>IoT.DeviceOffline</c> notification to the device's followers.
    /// </summary>
    public static async Task HandleAsync(
        DeviceOfflineDetectedEto message,
        INotificationPublisher publisher,
        IoTMetrics metrics,
        ILogger<DeviceOfflineNotificationHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        metrics.RecordDeviceOfflineDetected(message.TenantId?.ToString());

        var data = new IoTDeviceOfflineData(
            message.DeviceId,
            message.LastHeartbeatAt ?? DateTimeOffset.MinValue);
        var entity = new EntityReference(DeviceEntityType, message.DeviceId.ToString("N"));

        await publisher
            .PublishToEntityFollowersAsync(
                IoTDeviceOfflineNotificationType.Instance,
                data,
                entity,
                cancellationToken)
            .ConfigureAwait(false);

        LogDeviceOfflinePublished(logger, message.DeviceId, message.SerialNumber);
    }

    [LoggerMessage(EventId = 4304, Level = LogLevel.Information,
        Message = "IoT device offline notification published for device {DeviceId} ({SerialNumber}).")]
    private static partial void LogDeviceOfflinePublished(ILogger logger, Guid deviceId, string serialNumber);
}

/// <summary>Marker type for the static handler's logger category.</summary>
public sealed class DeviceOfflineNotificationHandlerCategory;
