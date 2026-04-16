using Granit.IoT.Ingestion.Endpoints.Endpoints;
using Granit.RateLimiting.AspNetCore;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.IoT.Ingestion.Endpoints.Extensions;

public static class IoTIngestionEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Default rate-limit policy name applied to <c>POST /iot/ingest/{source}</c>.
    /// Configure under <c>RateLimiting:Policies:iot-ingest</c> in <c>appsettings.json</c>.
    /// </summary>
    public const string DefaultRateLimitPolicy = "iot-ingest";

    /// <summary>
    /// Maps the IoT ingestion webhook endpoint group at <c>/iot/ingest</c>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="rateLimitPolicy">
    /// Name of the rate-limit policy to apply. Defaults to <see cref="DefaultRateLimitPolicy"/>.
    /// </param>
    public static IEndpointRouteBuilder MapGranitIoTIngestionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string rateLimitPolicy = DefaultRateLimitPolicy)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(rateLimitPolicy);

        RouteGroupBuilder ingestion = endpoints
            .MapGranitGroup("/iot/ingest")
            .WithTags("IoT Ingestion")
            .RequireGranitRateLimiting(rateLimitPolicy);

        ingestion.MapIngestionRoutes();

        return endpoints;
    }
}
