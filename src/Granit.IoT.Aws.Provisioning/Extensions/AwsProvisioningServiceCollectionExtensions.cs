using Amazon.IoT;
using Amazon.SecretsManager;
using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Provisioning.Abstractions;
using Granit.IoT.Aws.Provisioning.Diagnostics;
using Granit.IoT.Aws.Provisioning.Internal;
using Granit.IoT.Aws.Provisioning.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Provisioning.Extensions;

public static class AwsProvisioningServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AWS provisioning saga: <see cref="IThingProvisioningService"/>,
    /// the Secrets Manager-backed <see cref="IAwsIoTCredentialLoader"/>, the
    /// observability counters, and the AWS SDK clients themselves.
    /// </summary>
    public static IServiceCollection AddGranitIoTAwsProvisioning(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AwsThingProvisioningOptions>()
            .BindConfiguration(AwsThingProvisioningOptions.SectionName)
            .ValidateDataAnnotations();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AwsThingProvisioningOptions>, AwsThingProvisioningOptionsValidator>());

        services.TryAddSingleton<AwsProvisioningMetrics>();
        services.TryAddScoped<IThingProvisioningService, ThingProvisioningService>();
        services.TryAddScoped<IAwsIoTCredentialLoader, AwsSecretsManagerCredentialLoader>();

        // The AWS SDK clients use the application-wide credential chain.
        // Hosts that need fleet-rotated credentials should override these
        // registrations with factory delegates that consume
        // IAwsIoTCredentialProvider — see the package README for an example.
        services.TryAddSingleton<IAmazonIoT>(_ => new AmazonIoTClient());
        services.TryAddSingleton<IAmazonSecretsManager>(_ => new AmazonSecretsManagerClient());

        return services;
    }
}
