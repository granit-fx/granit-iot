namespace Granit.IoT.Endpoints.Dtos;

public sealed record DeviceProvisionRequest
{
    public required string SerialNumber { get; init; }
    public required string HardwareModel { get; init; }
    public required string FirmwareVersion { get; init; }
    public string? Label { get; init; }
}
