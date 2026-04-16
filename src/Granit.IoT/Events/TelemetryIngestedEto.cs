using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Integration event published when an inbound telemetry payload has been validated,
/// parsed, and accepted for asynchronous persistence by the Wolverine handler.
/// </summary>
/// <param name="MessageId">Transport-level message identifier (already deduplicated).</param>
/// <param name="DeviceExternalId">Device serial number as exposed by the IoT hub.</param>
/// <param name="DeviceId">Resolved internal device identifier (<see langword="null"/> if not yet known).</param>
/// <param name="TenantId">Tenant of the resolved device (<see langword="null"/> if not yet known).</param>
/// <param name="RecordedAt">Timestamp at which the device emitted the payload.</param>
/// <param name="Metrics">Metric name/value pairs.</param>
/// <param name="Source">Provider source identifier (e.g. <c>"scaleway"</c>).</param>
/// <param name="Tags">Optional device-supplied tags.</param>
public sealed record TelemetryIngestedEto(
    string MessageId,
    string DeviceExternalId,
    Guid? DeviceId,
    Guid? TenantId,
    DateTimeOffset RecordedAt,
    IReadOnlyDictionary<string, double> Metrics,
    string Source,
    IReadOnlyDictionary<string, string>? Tags) : IIntegrationEvent;
