namespace Granit.IoT.Mqtt.Internal;

/// <summary>
/// Default <see cref="IIoTMqttBridge"/> registration — a no-op so consumers that load
/// <c>GranitIoTMqttModule</c> for the abstractions (parser, validator, settings) but
/// do not also load <c>Granit.IoT.Mqtt.Mqttnet</c> get a deterministic, harmless
/// implementation rather than a DI failure.
/// </summary>
internal sealed class NullIoTMqttBridge : IIoTMqttBridge
{
    public MqttBridgeStatus Status => MqttBridgeStatus.Stopped;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
