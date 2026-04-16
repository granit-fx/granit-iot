using Granit.IoT.Endpoints.Endpoints;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.IoT.Endpoints.Extensions;

public static class IoTEndpointRouteBuilderExtensions
{
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
