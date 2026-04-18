using Granit.Domain;
using Granit.IoT.Diagnostics;
using Granit.IoT.Events;
using Granit.IoT.Notifications.Abstractions;
using Granit.Notifications.Abstractions;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Notifications.Handlers;

/// <summary>
/// Wolverine handler that turns each <see cref="TelemetryThresholdExceededEto"/>
/// into a <c>IoT.TelemetryThresholdAlert</c> notification delivered to the followers
/// of the originating device. Per-(device, metric) throttling prevents alert fatigue
/// when a value oscillates around its threshold.
/// </summary>
public static partial class TelemetryThresholdNotificationHandler
{
    private const string DeviceEntityType = "Device";

    /// <summary>
    /// Handles <see cref="TelemetryThresholdExceededEto"/> — acquires the
    /// per-(device, metric) throttle and publishes a
    /// <c>IoT.TelemetryThresholdAlert</c> notification when admitted.
    /// </summary>
    public static async Task HandleAsync(
        TelemetryThresholdExceededEto message,
        IAlertThrottle throttle,
        INotificationPublisher publisher,
        IoTMetrics metrics,
        ILogger<TelemetryThresholdNotificationHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        bool acquired = await throttle
            .TryAcquireAsync(message.DeviceId, message.MetricName, message.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (!acquired)
        {
            metrics.RecordAlertThrottled(message.TenantId?.ToString(), message.MetricName);
            LogAlertThrottled(logger, message.DeviceId, message.MetricName);
            return;
        }

        var data = new IoTTelemetryThresholdAlertData(
            message.DeviceId,
            message.MetricName,
            message.ObservedValue,
            message.ThresholdValue,
            message.RecordedAt);

        var entity = new EntityReference(DeviceEntityType, message.DeviceId.ToString("N"));

        await publisher
            .PublishToEntityFollowersAsync(
                IoTTelemetryThresholdAlertNotificationType.Instance,
                data,
                entity,
                cancellationToken)
            .ConfigureAwait(false);

        LogThresholdAlertPublished(logger, message.DeviceId, message.MetricName, message.ObservedValue, message.ThresholdValue);
    }

    [LoggerMessage(EventId = 4302, Level = LogLevel.Information, Message = "IoT threshold alert published for device {DeviceId} metric '{MetricName}' (observed {ObservedValue}, threshold {ThresholdValue}).")]
    private static partial void LogThresholdAlertPublished(ILogger logger, Guid deviceId, string metricName, double observedValue, double thresholdValue);

    [LoggerMessage(EventId = 4303, Level = LogLevel.Debug, Message = "IoT threshold alert throttled for device {DeviceId} metric '{MetricName}'.")]
    private static partial void LogAlertThrottled(ILogger logger, Guid deviceId, string metricName);
}

/// <summary>
/// Marker type for <see cref="ILogger{TCategoryName}"/> binding. Provides a stable
/// non-static category for the static <see cref="TelemetryThresholdNotificationHandler"/>.
/// </summary>
public sealed class TelemetryThresholdNotificationHandlerCategory;
