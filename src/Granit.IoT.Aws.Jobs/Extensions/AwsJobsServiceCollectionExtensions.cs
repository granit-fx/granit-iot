using Granit.IoT.Aws.Jobs.Abstractions;
using Granit.IoT.Aws.Jobs.Diagnostics;
using Granit.IoT.Aws.Jobs.Internal;
using Granit.IoT.Aws.Jobs.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Aws.Jobs.Extensions;

/// <summary>
/// Service-collection extensions for the AWS IoT Jobs satellite
/// (<c>Granit.IoT.Aws.Jobs</c>).
/// </summary>
public static class AwsJobsServiceCollectionExtensions
{
    /// <summary>
    /// Wires the AWS IoT Jobs dispatcher, the in-memory tracking store and
    /// the status polling service. Hosts that scale out horizontally should
    /// override <see cref="IJobTrackingStore"/> with a Redis-backed
    /// implementation (the in-memory default keeps each host's tracking
    /// scope local).
    /// </summary>
    public static IServiceCollection AddGranitIoTAwsJobs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AwsIoTJobsOptions>()
            .BindConfiguration(AwsIoTJobsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IoTAwsJobsMetrics>();
        services.TryAddSingleton<IJobTrackingStore, InMemoryJobTrackingStore>();
        services.TryAddScoped<IDeviceCommandDispatcher, AwsIoTJobsCommandDispatcher>();
        services.AddHostedService<IoTJobStatusPollingService>();

        return services;
    }
}
