namespace Granit.IoT.Mqtt;

/// <summary>
/// Transport adapter that subscribes to an MQTT broker and routes inbound messages
/// through the existing <c>Granit.IoT.Ingestion</c> pipeline. The bridge is the only
/// MQTT-specific surface the host application sees — parsing, deduplication, device
/// resolution and outbox publishing all reuse the HTTP-webhook code path.
/// </summary>
/// <remarks>
/// The default registration is <c>NullIoTMqttBridge</c> (no-op). The MQTTnet v5
/// implementation in <c>Granit.IoT.Mqtt.Mqttnet</c> replaces it via
/// <c>services.Replace(...)</c> and is also registered as an
/// <see cref="Microsoft.Extensions.Hosting.IHostedService"/> so the broker connection
/// follows the host lifecycle.
/// </remarks>
public interface IIoTMqttBridge
{
    /// <summary>Current lifecycle state.</summary>
    MqttBridgeStatus Status { get; }

    /// <summary>Connect to the broker and subscribe to the configured topics.</summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>Disconnect cleanly from the broker.</summary>
    Task StopAsync(CancellationToken cancellationToken);
}
