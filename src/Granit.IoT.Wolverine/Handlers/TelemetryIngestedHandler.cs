using Granit.Events;
using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Diagnostics;
using Granit.IoT.Domain;
using Granit.IoT.Events;
using Granit.IoT.Wolverine.Abstractions;
using Granit.MultiTenancy;
using Granit.Timing;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Wolverine.Handlers;

/// <summary>
/// Wolverine handler for <see cref="TelemetryIngestedEto"/>. Persists the telemetry point,
/// updates the device heartbeat, and publishes <see cref="TelemetryThresholdExceededEto"/>
/// for any breached metric. Wolverine wraps the call in a single transaction so all side
/// effects commit atomically (or fail and retry).
/// </summary>
public static partial class TelemetryIngestedHandler
{
    public static async Task HandleAsync(
        TelemetryIngestedEto message,
        ITelemetryWriter telemetryWriter,
        IDeviceWriter deviceWriter,
        IDeviceThresholdEvaluator thresholdEvaluator,
        IDistributedEventBus eventBus,
        IGuidGenerator guidGenerator,
        IClock clock,
        IoTMetrics metrics,
        ICurrentTenant currentTenant,
        ILogger<TelemetryIngestedHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.DeviceId is not { } deviceId)
        {
            metrics.RecordIngestionUnknownDevice(message.TenantId?.ToString(), message.Source);
            LogUnknownDeviceMessage(logger, message.MessageId, message.DeviceExternalId, message.Source);
            return;
        }

        using IDisposable _ = currentTenant.Change(message.TenantId);

        var point = TelemetryPoint.Create(
            id: guidGenerator.Create(),
            deviceId: deviceId,
            tenantId: message.TenantId,
            recordedAt: message.RecordedAt,
            metrics: message.Metrics,
            messageId: message.MessageId,
            source: message.Source);

        await telemetryWriter.AppendAsync(point, cancellationToken).ConfigureAwait(false);
        await deviceWriter
            .UpdateHeartbeatAsync(deviceId, clock.Now, cancellationToken)
            .ConfigureAwait(false);

        metrics.RecordTelemetryIngested(message.TenantId?.ToString(), message.Source);

        IReadOnlyList<TelemetryThresholdExceededEto> breaches = await thresholdEvaluator
            .EvaluateAsync(deviceId, message.TenantId, message.Metrics, message.RecordedAt, cancellationToken)
            .ConfigureAwait(false);

        foreach (TelemetryThresholdExceededEto breach in breaches)
        {
            metrics.RecordIngestionThresholdExceeded(message.TenantId?.ToString(), breach.MetricName);
            await eventBus.PublishAsync(breach, cancellationToken).ConfigureAwait(false);
        }
    }
}

public static partial class TelemetryIngestedHandler
{
    [LoggerMessage(EventId = 4201, Level = LogLevel.Warning, Message = "Telemetry ingested message '{MessageId}' references unknown device '{DeviceExternalId}' on source '{Source}'. Skipping.")]
    internal static partial void LogUnknownDeviceMessage(ILogger logger, string messageId, string deviceExternalId, string source);
}

/// <summary>
/// Marker type for <see cref="ILogger{TCategoryName}"/> binding. Provides a stable
/// non-static category for the static <see cref="TelemetryIngestedHandler"/>.
/// </summary>
public sealed class TelemetryIngestedHandlerCategory;
