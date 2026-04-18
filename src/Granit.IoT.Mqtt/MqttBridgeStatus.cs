namespace Granit.IoT.Mqtt;

/// <summary>
/// Lifecycle state of an <see cref="IIoTMqttBridge"/>.
/// </summary>
public enum MqttBridgeStatus
{
    /// <summary>Initial and post-shutdown state. No MQTT connection is open.</summary>
    Stopped,

    /// <summary>Bridge is initializing — certificate is loading or first connect is in flight.</summary>
    Starting,

    /// <summary>Connected to the broker and subscribed to the configured topic pattern.</summary>
    Connected,

    /// <summary>Lost the broker connection; the reconnect pipeline is attempting to re-establish it.</summary>
    Reconnecting,

    /// <summary>Reconnect policy exhausted or the certificate loader failed permanently. Requires operator intervention.</summary>
    Faulted,
}
