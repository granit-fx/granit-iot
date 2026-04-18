using Granit.IoT.Aws.Shadow.Abstractions;
using Granit.IoT.Aws.Shadow.Diagnostics;
using Granit.IoT.Aws.Shadow.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Aws.Shadow.Tests.Extensions;

public sealed class AwsShadowServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTAwsShadow_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTAwsShadow());
    }

    [Fact]
    public void AddGranitIoTAwsShadow_RegistersAllServices()
    {
        ServiceCollection services = NewServices();

        services.AddGranitIoTAwsShadow();

        services.ShouldContain(d => d.ServiceType == typeof(IoTAwsShadowMetrics));
        services.ShouldContain(d => d.ServiceType == typeof(IDeviceShadowSyncService));
        services.ShouldContain(d => d.ServiceType == typeof(TimeProvider));
        services.ShouldContain(d => d.ServiceType == typeof(Amazon.IotData.IAmazonIotData));
    }

    private static ServiceCollection NewServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        return services;
    }
}
