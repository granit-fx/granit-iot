using Granit.IoT.Endpoints.Endpoints;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.IoT.Endpoints.Extensions;

/// <summary>
/// Endpoint route-builder extensions that map the core IoT endpoints
/// (<c>/iot/devices</c>, <c>/iot/telemetry</c>) onto the host pipeline.
/// </summary>
public static class IoTEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the core IoT endpoint groups: <c>/iot/devices</c> (device CRUD + lifecycle)
    /// and <c>/iot/telemetry</c> (query + latest reading). Both groups require
    /// authorization.
    /// </summary>
    public static IEndpointRouteBuilder MapGranitIoTEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder devices = endpoints
            .MapGranitGroup("/iot/devices")
            .RequireAuthorization()
            .WithTags("IoT Devices");
        devices.MapDeviceRoutes();

        RouteGroupBuilder telemetry = endpoints
            .MapGranitGroup("/iot/telemetry")
            .RequireAuthorization()
            .WithTags("IoT Telemetry");
        telemetry.MapTelemetryRoutes();

        return endpoints;
    }
}
