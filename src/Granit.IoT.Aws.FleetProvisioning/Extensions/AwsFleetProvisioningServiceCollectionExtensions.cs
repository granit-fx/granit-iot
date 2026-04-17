using Granit.IoT.Aws.FleetProvisioning.Abstractions;
using Granit.IoT.Aws.FleetProvisioning.Diagnostics;
using Granit.IoT.Aws.FleetProvisioning.Internal;
using Granit.IoT.Aws.FleetProvisioning.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Aws.FleetProvisioning.Extensions;

public static class AwsFleetProvisioningServiceCollectionExtensions
{
    /// <summary>
    /// Wires the JITP service, the rotation check background sweep and
    /// the observability counters. The endpoints themselves are mapped via
    /// <see cref="FleetProvisioningEndpointRouteBuilderExtensions.MapGranitIoTAwsFleetProvisioningEndpoints"/>.
    /// </summary>
    public static IServiceCollection AddGranitIoTAwsFleetProvisioning(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<FleetProvisioningOptions>()
            .BindConfiguration(FleetProvisioningOptions.SectionName)
            .ValidateDataAnnotations();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<FleetProvisioningMetrics>();
        services.TryAddScoped<IFleetProvisioningService, FleetProvisioningService>();
        services.AddHostedService<ClaimCertificateRotationCheckService>();

        return services;
    }
}
