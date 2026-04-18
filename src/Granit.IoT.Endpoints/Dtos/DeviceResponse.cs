namespace Granit.IoT.Endpoints.Dtos;

/// <summary>
/// Device projection returned by GET device endpoints. Mirrors the aggregate state
/// that is safe to expose over HTTP — private fields (<c>Credential</c>, tenant
/// binding, suspension reason) are excluded.
/// </summary>
/// <param name="Id">Device identifier (UUID v7).</param>
/// <param name="SerialNumber">Manufacturer-supplied serial number. Unique per tenant.</param>
/// <param name="HardwareModel">Hardware model identifier (e.g. <c>"TempProbe-v2"</c>).</param>
/// <param name="FirmwareVersion">Currently installed firmware version (semver).</param>
/// <param name="Status">Current lifecycle status: <c>Provisioning</c>, <c>Active</c>, <c>Suspended</c>, <c>Decommissioned</c>.</param>
/// <param name="Label">Optional operator-supplied label. <c>null</c> when unset.</param>
/// <param name="LastHeartbeatAt">Timestamp of the last observed heartbeat. <c>null</c> for devices that have never reported.</param>
/// <param name="CreatedAt">Provisioning timestamp (audit trail).</param>
/// <param name="ModifiedAt">Last modification timestamp. <c>null</c> when the device has never been updated.</param>
public sealed record DeviceResponse(
    Guid Id,
    string SerialNumber,
    string HardwareModel,
    string FirmwareVersion,
    string Status,
    string? Label,
    DateTimeOffset? LastHeartbeatAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);
