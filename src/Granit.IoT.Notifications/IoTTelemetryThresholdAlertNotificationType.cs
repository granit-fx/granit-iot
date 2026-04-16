using Granit.Notifications;

namespace Granit.IoT.Notifications;

/// <summary>
/// Sent when a telemetry metric breaches its configured threshold.
/// Delivered to the followers of the originating <c>Device</c> entity.
/// </summary>
public sealed class IoTTelemetryThresholdAlertNotificationType
    : NotificationType<IoTTelemetryThresholdAlertData>
{
    /// <summary>Singleton instance.</summary>
    public static readonly IoTTelemetryThresholdAlertNotificationType Instance = new();

    /// <inheritdoc/>
    public override string Name => "IoT.TelemetryThresholdAlert";

    /// <inheritdoc/>
    public override NotificationSeverity DefaultSeverity => NotificationSeverity.Warning;

    /// <inheritdoc/>
    public override IReadOnlyList<string> DefaultChannels { get; } =
        [NotificationChannels.Email, NotificationChannels.Push];
}

/// <summary>Payload for <see cref="IoTTelemetryThresholdAlertNotificationType"/>.</summary>
/// <param name="DeviceId">Internal identifier of the device that emitted the breaching metric.</param>
/// <param name="MetricName">Name of the metric that breached the threshold.</param>
/// <param name="ObservedValue">Measured value that exceeded the threshold.</param>
/// <param name="ThresholdValue">Configured threshold value.</param>
/// <param name="RecordedAt">Timestamp at which the device emitted the payload.</param>
public sealed record IoTTelemetryThresholdAlertData(
    Guid DeviceId,
    string MetricName,
    double ObservedValue,
    double ThresholdValue,
    DateTimeOffset RecordedAt);
