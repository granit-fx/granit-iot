namespace Granit.IoT.Endpoints.Dtos;

public sealed record TelemetryPointResponse(
    Guid Id,
    Guid DeviceId,
    DateTimeOffset RecordedAt,
    IReadOnlyDictionary<string, double> Metrics,
    string? Source);
