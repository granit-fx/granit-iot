using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Integration event published when an inbound telemetry payload references a device
/// serial number that does not match any provisioned device. Allows downstream consumers
/// (auto-provisioning, alerting) to react asynchronously.
/// </summary>
/// <param name="MessageId">Transport-level message identifier.</param>
/// <param name="DeviceExternalId">Unrecognized device serial number from the inbound payload.</param>
/// <param name="Source">Provider source identifier (e.g. <c>"scaleway"</c>).</param>
/// <param name="ObservedAt">Timestamp at which the unknown device was observed.</param>
public sealed record DeviceUnknownEto(
    string MessageId,
    string DeviceExternalId,
    string Source,
    DateTimeOffset ObservedAt) : IIntegrationEvent;
