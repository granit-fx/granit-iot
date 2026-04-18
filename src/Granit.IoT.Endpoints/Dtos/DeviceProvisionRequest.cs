namespace Granit.IoT.Endpoints.Dtos;

/// <summary>
/// Payload for <c>POST /devices</c>. Provisioning is idempotent on
/// <see cref="SerialNumber"/> within a tenant — re-provisioning the same serial
/// returns the existing device rather than creating a duplicate.
/// </summary>
public sealed record DeviceProvisionRequest
{
    /// <summary>Manufacturer-supplied serial number. Unique per tenant.</summary>
    public required string SerialNumber { get; init; }

    /// <summary>Hardware model identifier (e.g. <c>"TempProbe-v2"</c>).</summary>
    public required string HardwareModel { get; init; }

    /// <summary>Initial firmware version (semver) installed at provisioning time.</summary>
    public required string FirmwareVersion { get; init; }

    /// <summary>Optional human-readable label. Tenant-supplied free text, sanitized before exposure to AI agents.</summary>
    public string? Label { get; init; }
}
