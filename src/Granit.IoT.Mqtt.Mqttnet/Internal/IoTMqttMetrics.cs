using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Granit.IoT.Mqtt.Mqttnet.Internal;

/// <summary>
/// OpenTelemetry metrics emitted by the MQTTnet bridge. Separate meter (and counter
/// names) from <c>Granit.IoT</c> so MQTT-specific dashboards don't have to filter by tag.
/// </summary>
internal sealed class IoTMqttMetrics
{
    public const string MeterName = "Granit.IoT.Mqtt";

    private readonly Counter<long> _messagesReceived;
    private readonly Counter<long> _messagesDispatched;
    private readonly Counter<long> _featureDisabled;
    private readonly Counter<long> _connectionFailures;
    private readonly Counter<long> _reconnectAttempts;
    private readonly Counter<long> _certificateReloads;

    public IoTMqttMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);

        _messagesReceived = meter.CreateCounter<long>(
            "granit.iot.mqtt.messages_received",
            description: "Raw inbound MQTT messages observed by the bridge.");
        _messagesDispatched = meter.CreateCounter<long>(
            "granit.iot.mqtt.messages_dispatched",
            description: "MQTT messages forwarded to the ingestion pipeline (tagged with the pipeline outcome).");
        _featureDisabled = meter.CreateCounter<long>(
            "granit.iot.mqtt.feature_disabled",
            description: "MQTT messages dropped because the IoT.MqttBridge feature flag was disabled.");
        _connectionFailures = meter.CreateCounter<long>(
            "granit.iot.mqtt.connection_failures",
            description: "Connect or reconnect attempts that threw at the broker.");
        _reconnectAttempts = meter.CreateCounter<long>(
            "granit.iot.mqtt.reconnect_attempts",
            description: "Polly retry callbacks fired during reconnect.");
        _certificateReloads = meter.CreateCounter<long>(
            "granit.iot.mqtt.certificate_reloads",
            description: "Proactive certificate reloads triggered by the ExpiresOn timer.");
    }

    public void RecordReceived() => _messagesReceived.Add(1);

    public void RecordDispatched(string outcome) =>
        _messagesDispatched.Add(1, new TagList { { "outcome", outcome } });

    public void RecordFeatureDisabled() => _featureDisabled.Add(1);

    public void RecordConnectionFailure() => _connectionFailures.Add(1);

    public void RecordReconnectAttempt() => _reconnectAttempts.Add(1);

    public void RecordCertificateReload() => _certificateReloads.Add(1);
}
