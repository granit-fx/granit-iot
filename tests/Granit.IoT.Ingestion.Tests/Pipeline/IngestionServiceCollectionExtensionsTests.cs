using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Extensions;
using Granit.IoT.Ingestion.Scaleway.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Pipeline;

public sealed class IngestionServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTIngestion_NullServices_Throws()
    {
        IServiceCollection? services = null;
        IHostEnvironment env = StubEnv("Production");

        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTIngestion(env));
    }

    [Fact]
    public void AddGranitIoTIngestion_NullEnv_Throws()
    {
        ServiceCollection services = new();

        Should.Throw<ArgumentNullException>(() => services.AddGranitIoTIngestion(null!));
    }

    [Fact]
    public void AddGranitIoTIngestion_RegistersPipelineAndDeduplicator()
    {
        ServiceCollection services = NewBaseServices();

        services.AddGranitIoTIngestion(StubEnv("Production"));

        services.ShouldContain(d => d.ServiceType == typeof(IInboundMessageDeduplicator));
        services.ShouldContain(d => d.ServiceType == typeof(IIngestionPipeline));
    }

    [Fact]
    public void AddGranitIoTIngestion_DevelopmentEnv_RegistersNullSignatureValidator()
    {
        ServiceCollection services = NewBaseServices();

        services.AddGranitIoTIngestion(StubEnv("Development"));

        services.ShouldContain(d => d.ServiceType == typeof(IPayloadSignatureValidator));
    }

    [Fact]
    public void AddGranitIoTIngestion_ProductionEnv_DoesNotRegisterNullValidator()
    {
        ServiceCollection services = NewBaseServices();

        services.AddGranitIoTIngestion(StubEnv("Production"));

        services.ShouldNotContain(d => d.ServiceType == typeof(IPayloadSignatureValidator));
    }

    [Fact]
    public void AddGranitIoTIngestionScaleway_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTIngestionScaleway());
    }

    [Fact]
    public void AddGranitIoTIngestionScaleway_RegistersAllScalewayServices()
    {
        ServiceCollection services = NewBaseServices();

        services.AddGranitIoTIngestionScaleway();

        services.ShouldContain(d => d.ServiceType == typeof(IPayloadSignatureValidator));
        services.ShouldContain(d => d.ServiceType == typeof(IInboundMessageParser));
    }

    private static IHostEnvironment StubEnv(string envName)
    {
        IHostEnvironment env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(envName);
        return env;
    }

    private static ServiceCollection NewBaseServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        return services;
    }
}
