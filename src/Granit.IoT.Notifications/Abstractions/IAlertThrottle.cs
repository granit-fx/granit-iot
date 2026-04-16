namespace Granit.IoT.Notifications.Abstractions;

/// <summary>
/// Per-(device, metric) debounce so threshold notifications do not flood when a
/// telemetry value oscillates around its configured threshold. Returns
/// <see langword="true"/> when the caller should publish (no recent alert) and
/// <see langword="false"/> when an alert is still inside the throttle window.
/// </summary>
public interface IAlertThrottle
{
    /// <summary>
    /// Atomically checks the throttle window and reserves a slot for a new alert.
    /// Implementations should fail open on cache outage so transient infrastructure
    /// problems never suppress an alert.
    /// </summary>
    /// <param name="deviceId">Device that emitted the breaching metric.</param>
    /// <param name="metricName">Metric that breached its threshold.</param>
    /// <param name="tenantId">Tenant of the device (or <see langword="null"/> for global devices) — used for diagnostics only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the alert should be published, <see langword="false"/> when throttled.</returns>
    Task<bool> TryAcquireAsync(Guid deviceId, string metricName, Guid? tenantId, CancellationToken cancellationToken);
}
