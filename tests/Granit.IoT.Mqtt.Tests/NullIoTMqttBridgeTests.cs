using Granit.IoT.Mqtt;
using Granit.IoT.Mqtt.Internal;
using Shouldly;

namespace Granit.IoT.Mqtt.Tests;

public sealed class NullIoTMqttBridgeTests
{
    [Fact]
    public void Status_IsStopped() =>
        new NullIoTMqttBridge().Status.ShouldBe(MqttBridgeStatus.Stopped);

    [Fact]
    public async Task StartAsync_IsNoop()
    {
        NullIoTMqttBridge bridge = new();
        await bridge.StartAsync(TestContext.Current.CancellationToken);
        bridge.Status.ShouldBe(MqttBridgeStatus.Stopped);
    }

    [Fact]
    public async Task StopAsync_IsNoop()
    {
        NullIoTMqttBridge bridge = new();
        await bridge.StopAsync(TestContext.Current.CancellationToken);
        bridge.Status.ShouldBe(MqttBridgeStatus.Stopped);
    }
}
