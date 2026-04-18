using Granit.IoT.Aws.FleetProvisioning.Endpoints;
using Granit.RateLimiting.AspNetCore;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.IoT.Aws.FleetProvisioning.Extensions;

/// <summary>
/// Endpoint route-builder extensions for mapping the AWS Fleet Provisioning
/// (JITP) endpoints onto the host pipeline.
/// </summary>
public static class FleetProvisioningEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Default rate-limit policy name applied to the JITP endpoints.
    /// Configure under <c>RateLimiting:Policies:iot-jitp</c> in <c>appsettings.json</c>.
    /// </summary>
    public const string DefaultRateLimitPolicy = "iot-jitp";

    /// <summary>
    /// Maps the AWS Fleet Provisioning (JITP) endpoint group at
    /// <c>/api/iot/fleet-provisioning</c>: <c>POST /verify</c> and <c>POST /registered</c>.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="authorizationPolicyName">
    /// Authorization policy that authenticates the customer's AWS Lambda (mTLS,
    /// HMAC-on-JWT, or a dedicated machine-to-machine principal). <b>Required</b>:
    /// the JITP payload carries a <c>TenantId</c> that the service cross-checks
    /// against the authenticated principal. Passing a null or whitespace policy
    /// name is a configuration error — the call throws at startup so the failure
    /// surfaces during <c>ValidateOnStart</c>, not at first request.
    /// </param>
    /// <param name="rateLimitPolicyName">
    /// Rate-limit policy name applied to the group. Defaults to
    /// <see cref="DefaultRateLimitPolicy"/>. JITP fires only at device onboarding so
    /// the policy can be tighter than the telemetry ingestion one.
    /// </param>
    public static IEndpointRouteBuilder MapGranitIoTAwsFleetProvisioningEndpoints(
        this IEndpointRouteBuilder endpoints,
        string authorizationPolicyName,
        string rateLimitPolicyName = DefaultRateLimitPolicy)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationPolicyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rateLimitPolicyName);

        RouteGroupBuilder group = endpoints
            .MapGranitGroup("/api/iot/fleet-provisioning")
            .WithTags("AWS IoT Fleet Provisioning")
            .RequireAuthorization(authorizationPolicyName)
            .RequireGranitRateLimiting(rateLimitPolicyName);

        group.MapFleetProvisioningRoutes();

        return endpoints;
    }
}
