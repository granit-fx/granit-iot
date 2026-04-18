using Granit.IoT.Mqtt.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Mqtt.Tests.Extensions;

public sealed class MqttServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTMqtt_NullServices_Throws()
    {
        IServiceCollection? services = null;

        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTMqtt());
    }

    [Fact]
    public void AddGranitIoTMqtt_RegistersAllExpectedServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddGranitIoTMqtt();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<Granit.IoT.Ingestion.Abstractions.IInboundMessageParser>().ShouldNotBeEmpty();
        provider.GetServices<Granit.IoT.Ingestion.Abstractions.IPayloadSignatureValidator>().ShouldNotBeEmpty();
    }

    [Fact]
    public void AddGranitIoTMqtt_RegistersDefaultBridge()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddGranitIoTMqtt();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<Granit.IoT.Mqtt.IIoTMqttBridge>().ShouldNotBeNull();
    }
}
