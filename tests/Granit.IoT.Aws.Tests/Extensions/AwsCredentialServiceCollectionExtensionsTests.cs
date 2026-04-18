using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Granit.IoT.Aws.Tests.Extensions;

public sealed class AwsCredentialServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTAwsCredentials_NullServices_Throws()
    {
        IServiceCollection? services = null;

        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTAwsCredentials());
    }

    [Fact]
    public void AddGranitIoTAwsCredentials_NoFleetArn_RegistersIamRoleProvider()
    {
        ServiceCollection services = NewServices();
        services.AddGranitIoTAwsCredentials();
        services.Configure<AwsIoTCredentialOptions>(opts => opts.FleetCredentialSecretArn = null);
        ServiceProvider provider = services.BuildServiceProvider();

        IAwsIoTCredentialProvider impl = provider.GetRequiredService<IAwsIoTCredentialProvider>();
        impl.ShouldNotBeNull();
        impl.GetType().Name.ShouldContain("IamRole");
    }

    [Fact]
    public void AddGranitIoTAwsCredentials_RegistersOptions()
    {
        ServiceCollection services = NewServices();
        services.AddGranitIoTAwsCredentials();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<AwsIoTCredentialOptions>>().ShouldNotBeNull();
    }

    private static ServiceCollection NewServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        return services;
    }
}
