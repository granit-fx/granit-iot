using Granit.Events;

namespace Granit.IoT.Events;

/// <summary>
/// Published by <c>DeviceHeartbeatTimeoutJob</c> when an Active device's
/// <c>LastHeartbeatAt</c> is null or older than the per-tenant timeout.
/// Consumed by <c>Granit.IoT.Notifications</c> to emit the
/// <c>IoT.DeviceOffline</c> notification.
/// </summary>
public sealed record DeviceOfflineDetectedEto(
    Guid DeviceId,
    string SerialNumber,
    DateTimeOffset? LastHeartbeatAt,
    Guid? TenantId) : IIntegrationEvent;
