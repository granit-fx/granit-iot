using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.Endpoints.Dtos;
using Granit.IoT.Permissions;
using Granit.Timing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Granit.IoT.Endpoints.Endpoints;

internal static class TelemetryEndpoints
{
    internal static RouteGroupBuilder MapTelemetryRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/{deviceId:guid}", QueryTelemetryAsync)
            .WithName("QueryTelemetry")
            .WithSummary("Queries telemetry points for a device.")
            .WithDescription("Returns telemetry points within a time range, ordered by RecordedAt descending. Returns 404 if the device does not exist in the current tenant.")
            .Produces<IReadOnlyList<TelemetryPointResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(IoTPermissions.Telemetry.Read);

        group.MapGet("/{deviceId:guid}/latest", GetLatestTelemetryAsync)
            .WithName("GetLatestTelemetry")
            .WithSummary("Gets the latest telemetry point for a device.")
            .WithDescription("Returns the most recent telemetry point or 404 if no data exists.")
            .Produces<TelemetryPointResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(IoTPermissions.Telemetry.Read);

        group.MapGet("/{deviceId:guid}/aggregate", GetTelemetryAggregateAsync)
            .WithName("GetTelemetryAggregate")
            .WithSummary("Computes an aggregate over telemetry data.")
            .WithDescription("Computes Avg, Min, Max, or Count for a specific metric over a time range. The computation is pushed to the database.")
            .Produces<TelemetryAggregateResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(IoTPermissions.Telemetry.Read);

        return group;
    }

    internal static async Task<Results<Ok<IReadOnlyList<TelemetryPointResponse>>, NotFound>> QueryTelemetryAsync(
        Guid deviceId,
        [FromServices] IDeviceReader deviceReader,
        [FromServices] ITelemetryReader telemetryReader,
        [FromServices] IClock clock,
        DateTimeOffset? rangeStart,
        DateTimeOffset? rangeEnd,
        int? maxPoints,
        CancellationToken cancellationToken = default)
    {
        if (!await DeviceExistsAsync(deviceId, deviceReader, cancellationToken).ConfigureAwait(false))
        {
            return TypedResults.NotFound();
        }

        DateTimeOffset now = clock.Now;
        DateTimeOffset start = rangeStart ?? now.AddHours(-24);
        DateTimeOffset end = rangeEnd ?? now;
        int limit = Math.Clamp(maxPoints ?? 500, 1, 10000);

        IReadOnlyList<TelemetryPoint> points = await telemetryReader
            .QueryAsync(deviceId, start, end, limit, cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok<IReadOnlyList<TelemetryPointResponse>>(
            points.Select(ToResponse).ToList());
    }

    internal static async Task<Results<Ok<TelemetryPointResponse>, NotFound>> GetLatestTelemetryAsync(
        Guid deviceId,
        [FromServices] IDeviceReader deviceReader,
        [FromServices] ITelemetryReader telemetryReader,
        CancellationToken cancellationToken = default)
    {
        if (!await DeviceExistsAsync(deviceId, deviceReader, cancellationToken).ConfigureAwait(false))
        {
            return TypedResults.NotFound();
        }

        TelemetryPoint? point = await telemetryReader
            .GetLatestAsync(deviceId, cancellationToken)
            .ConfigureAwait(false);

        if (point is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToResponse(point));
    }

    internal static async Task<Results<Ok<TelemetryAggregateResponse>, NotFound>> GetTelemetryAggregateAsync(
        Guid deviceId,
        string metric,
        TelemetryAggregation aggregation,
        [FromServices] IDeviceReader deviceReader,
        [FromServices] ITelemetryReader telemetryReader,
        [FromServices] IClock clock,
        DateTimeOffset? rangeStart,
        DateTimeOffset? rangeEnd,
        CancellationToken cancellationToken = default)
    {
        if (!await DeviceExistsAsync(deviceId, deviceReader, cancellationToken).ConfigureAwait(false))
        {
            return TypedResults.NotFound();
        }

        DateTimeOffset now = clock.Now;
        DateTimeOffset start = rangeStart ?? now.AddHours(-24);
        DateTimeOffset end = rangeEnd ?? now;

        TelemetryAggregate? aggregate = await telemetryReader
            .GetAggregateAsync(deviceId, metric, start, end, aggregation, cancellationToken)
            .ConfigureAwait(false);

        if (aggregate is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new TelemetryAggregateResponse(
            aggregate.Value,
            aggregate.Count,
            metric,
            aggregation.ToString(),
            aggregate.RangeStart,
            aggregate.RangeEnd));
    }

    internal static async Task<bool> DeviceExistsAsync(
        Guid deviceId,
        IDeviceReader deviceReader,
        CancellationToken cancellationToken)
    {
        Device? device = await deviceReader.FindAsync(deviceId, cancellationToken).ConfigureAwait(false);
        return device is not null;
    }

    private static TelemetryPointResponse ToResponse(TelemetryPoint point) => new(
        point.Id,
        point.DeviceId,
        point.RecordedAt,
        point.Metrics,
        point.Source);
}
