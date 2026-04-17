using Granit.IoT.Aws.FleetProvisioning.Endpoints;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.IoT.Aws.FleetProvisioning.Extensions;

public static class FleetProvisioningEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the AWS Fleet Provisioning (JITP) endpoint group at
    /// <c>/api/iot/fleet-provisioning</c>:
    /// <c>POST /verify</c> and <c>POST /registered</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapGranitIoTAwsFleetProvisioningEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints
            .MapGranitGroup("/api/iot/fleet-provisioning")
            .WithTags("AWS IoT Fleet Provisioning");

        group.MapFleetProvisioningRoutes();

        return endpoints;
    }
}
