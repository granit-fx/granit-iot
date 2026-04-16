namespace Granit.IoT.Endpoints.Dtos;

public sealed record TelemetryAggregateResponse(
    double Value,
    long Count,
    string MetricName,
    string Aggregation,
    DateTimeOffset RangeStart,
    DateTimeOffset RangeEnd);
