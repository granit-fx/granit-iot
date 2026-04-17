using Amazon.IotData;
using Granit.IoT.Aws.Shadow.Abstractions;
using Granit.IoT.Aws.Shadow.Diagnostics;
using Granit.IoT.Aws.Shadow.Internal;
using Granit.IoT.Aws.Shadow.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Aws.Shadow.Extensions;

public static class AwsShadowServiceCollectionExtensions
{
    /// <summary>
    /// Wires the AWS Device Shadow bridge: the sync service, the
    /// lifecycle-driven push handlers, the periodic delta poller, and the
    /// observability counters. Hosts that prefer the event-driven path
    /// (IoT Rule → SNS) can disable the polling service via DI overrides
    /// after this call.
    /// </summary>
    public static IServiceCollection AddGranitIoTAwsShadow(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AwsShadowOptions>()
            .BindConfiguration(AwsShadowOptions.SectionName)
            .ValidateDataAnnotations();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AwsShadowMetrics>();
        services.TryAddScoped<IDeviceShadowSyncService, DefaultDeviceShadowSyncService>();

        // Default IoT Data plane client uses the application credential chain.
        // Hosts wiring the rotating credential provider should override this
        // factory and feed it BasicAWSCredentials from IAwsIoTCredentialProvider.
        // Production deployments should also set ServiceURL via the AWS IoT
        // Core "iot:Data-ATS" endpoint for the account/region.
        services.TryAddSingleton<IAmazonIotData>(_ => new AmazonIotDataClient(new AmazonIotDataConfig()));

        services.AddHostedService<ShadowDeltaPollingService>();

        return services;
    }
}
