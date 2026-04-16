namespace Granit.IoT.Endpoints.Dtos;

public sealed record DeviceUpdateRequest
{
    public string? FirmwareVersion { get; init; }
    public string? Label { get; init; }
}
