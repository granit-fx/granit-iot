namespace Granit.IoT.Endpoints.Dtos;

/// <summary>
/// Payload for <c>PATCH /devices/{id}</c>. Both fields are optional — <c>null</c>
/// means "leave unchanged". Immutable device state (serial number, hardware model,
/// tenant binding) is not mutable via this contract.
/// </summary>
public sealed record DeviceUpdateRequest
{
    /// <summary>New firmware version (semver). <c>null</c> to keep the current value.</summary>
    public string? FirmwareVersion { get; init; }

    /// <summary>New operator-supplied label. <c>null</c> to keep the current value; empty string clears it.</summary>
    public string? Label { get; init; }
}
