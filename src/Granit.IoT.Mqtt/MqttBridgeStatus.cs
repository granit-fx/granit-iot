namespace Granit.IoT.Mqtt;

/// <summary>
/// Lifecycle state of an <see cref="IIoTMqttBridge"/>.
/// </summary>
public enum MqttBridgeStatus
{
    Stopped,
    Starting,
    Connected,
    Reconnecting,
    Faulted,
}
