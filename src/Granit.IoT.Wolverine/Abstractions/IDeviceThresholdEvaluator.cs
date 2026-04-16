using Granit.IoT.Events;

namespace Granit.IoT.Wolverine.Abstractions;

/// <summary>
/// Evaluates per-tenant metric thresholds for an ingested telemetry payload and returns
/// the integration events that must be enqueued for downstream notification handling.
/// </summary>
public interface IDeviceThresholdEvaluator
{
    /// <summary>
    /// Returns one <see cref="TelemetryThresholdExceededEto"/> per metric that breached
    /// its configured threshold. Returns an empty collection when no thresholds are
    /// configured or none are breached.
    /// </summary>
    Task<IReadOnlyList<TelemetryThresholdExceededEto>> EvaluateAsync(
        Guid deviceId,
        Guid? tenantId,
        IReadOnlyDictionary<string, double> metrics,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken);
}
