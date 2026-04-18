using Granit.IoT.Mqtt;
using Granit.IoT.Mqtt.Mqttnet.Extensions;
using Granit.IoT.Mqtt.Mqttnet.Internal;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Mqtt.Mqttnet.Tests.Extensions;

public sealed class IoTMqttMqttnetServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTMqttMqttnet_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTMqttMqttnet());
    }

    [Fact]
    public void AddGranitIoTMqttMqttnet_RegistersAllServices()
    {
        ServiceCollection services = new();

        services.AddGranitIoTMqttMqttnet();

        services.ShouldContain(d => d.ServiceType == typeof(IoTMqttMetrics));
        services.ShouldContain(d => d.ServiceType == typeof(ICertificateLoader));
        services.ShouldContain(d => d.ServiceType == typeof(ISettingsTopicResolver));
        services.ShouldContain(d => d.ServiceType == typeof(IIoTMqttBridge));
        services.ShouldContain(d => d.ServiceType == typeof(MqttnetIoTBridge));
    }
}
