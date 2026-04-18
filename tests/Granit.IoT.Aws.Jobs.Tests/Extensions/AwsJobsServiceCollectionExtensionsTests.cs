using Granit.IoT.Aws.Jobs.Abstractions;
using Granit.IoT.Aws.Jobs.Diagnostics;
using Granit.IoT.Aws.Jobs.Extensions;
using Granit.IoT.Aws.Jobs.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Aws.Jobs.Tests.Extensions;

public sealed class AwsJobsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTAwsJobs_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTAwsJobs());
    }

    [Fact]
    public void AddGranitIoTAwsJobs_RegistersAllServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddGranitIoTAwsJobs();

        services.ShouldContain(d => d.ServiceType == typeof(IoTAwsJobsMetrics));
        services.ShouldContain(d => d.ServiceType == typeof(IJobTrackingStore));
        services.ShouldContain(d => d.ServiceType == typeof(IDeviceCommandDispatcher));
        services.ShouldContain(d => d.ServiceType == typeof(TimeProvider));
    }
}
