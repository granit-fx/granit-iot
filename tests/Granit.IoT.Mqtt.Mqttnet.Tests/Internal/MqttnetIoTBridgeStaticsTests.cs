using Granit.IoT.Mqtt.Mqttnet.Internal;
using MQTTnet.Protocol;
using Shouldly;

namespace Granit.IoT.Mqtt.Mqttnet.Tests.Internal;

public sealed class MqttnetIoTBridgeStaticsTests
{
    [Theory]
    [InlineData(0, MqttQualityOfServiceLevel.AtMostOnce)]
    [InlineData(1, MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(2, MqttQualityOfServiceLevel.ExactlyOnce)]
    [InlineData(99, MqttQualityOfServiceLevel.AtLeastOnce)]
    public void ToQos_MapsLevels(int level, MqttQualityOfServiceLevel expected)
    {
        MqttnetIoTBridge.ToQos(level).ShouldBe(expected);
    }

    [Fact]
    public void EnsureSecureBrokerUri_PlaintextScheme_Throws()
    {
        Should.Throw<InvalidOperationException>(() =>
            MqttnetIoTBridge.EnsureSecureBrokerUri("mqtt://broker.example.com:1883"));
    }

    [Fact]
    public void EnsureSecureBrokerUri_MqttsScheme_DoesNotThrow()
    {
        Should.NotThrow(() => MqttnetIoTBridge.EnsureSecureBrokerUri("mqtts://broker.example.com:8883"));
    }

    [Fact]
    public void EnsureSecureBrokerUri_InvalidUri_Throws()
    {
        Should.Throw<InvalidOperationException>(() =>
            MqttnetIoTBridge.EnsureSecureBrokerUri("not a uri"));
    }
}
