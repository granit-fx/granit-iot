using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Integration event published by the Wolverine handler when an ingested metric exceeds
/// a per-tenant threshold configured via <c>Granit.Settings</c>
/// (<c>IoT:Threshold:{metricName}</c>). Consumed downstream by notification publishers.
/// </summary>
/// <param name="DeviceId">Resolved internal device identifier.</param>
/// <param name="TenantId">Tenant of the device (or <see langword="null"/> for global devices).</param>
/// <param name="MetricName">Name of the metric that breached the threshold.</param>
/// <param name="ObservedValue">Measured value that exceeded the threshold.</param>
/// <param name="ThresholdValue">Configured threshold value.</param>
/// <param name="RecordedAt">Timestamp at which the device emitted the payload.</param>
public sealed record TelemetryThresholdExceededEto(
    Guid DeviceId,
    Guid? TenantId,
    string MetricName,
    double ObservedValue,
    double ThresholdValue,
    DateTimeOffset RecordedAt) : IIntegrationEvent;
