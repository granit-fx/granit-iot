using Amazon.IoT;
using Amazon.SecretsManager;
using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Provisioning.Abstractions;
using Granit.IoT.Aws.Provisioning.Diagnostics;
using Granit.IoT.Aws.Provisioning.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Aws.Provisioning.Tests.Extensions;

public sealed class AwsProvisioningServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTAwsProvisioning_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTAwsProvisioning());
    }

    [Fact]
    public void AddGranitIoTAwsProvisioning_RegistersAllServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddGranitIoTAwsProvisioning();

        services.ShouldContain(d => d.ServiceType == typeof(IoTAwsProvisioningMetrics));
        services.ShouldContain(d => d.ServiceType == typeof(IThingProvisioningService));
        services.ShouldContain(d => d.ServiceType == typeof(IAwsIoTCredentialLoader));
        services.ShouldContain(d => d.ServiceType == typeof(IAmazonIoT));
        services.ShouldContain(d => d.ServiceType == typeof(IAmazonSecretsManager));
    }
}
