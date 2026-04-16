namespace Granit.IoT.Endpoints.Dtos;

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
