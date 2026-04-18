using Granit.IoT.Aws.FleetProvisioning.Abstractions;
using Granit.IoT.Aws.FleetProvisioning.Diagnostics;
using Granit.IoT.Aws.FleetProvisioning.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Aws.FleetProvisioning.Tests.Extensions;

public sealed class AwsFleetProvisioningServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTAwsFleetProvisioning_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTAwsFleetProvisioning());
    }

    [Fact]
    public void AddGranitIoTAwsFleetProvisioning_RegistersAllServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddGranitIoTAwsFleetProvisioning();

        services.ShouldContain(d => d.ServiceType == typeof(IoTAwsFleetProvisioningMetrics));
        services.ShouldContain(d => d.ServiceType == typeof(IFleetProvisioningService));
        services.ShouldContain(d => d.ServiceType == typeof(TimeProvider));
    }
}
