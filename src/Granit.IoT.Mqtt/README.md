# Granit.IoT.Mqtt

**Why** — Some IoT deployments connect devices directly to an MQTT broker (factory
floor, edge gateways, on-prem) rather than routing through a cloud webhook. This
package lets the existing `Granit.IoT.Ingestion` pipeline accept those messages
unchanged: the MQTT bridge is a pure transport adapter that wraps each MQTT message
in an envelope and feeds it to `IIngestionPipeline.ProcessAsync`. Parsing,
deduplication, device resolution and outbox publishing all reuse the HTTP path.

This package contains the **abstraction only**:

- `IIoTMqttBridge` — `StartAsync` / `StopAsync` / `Status` lifecycle interface
- `MqttMessageParser` — implements `IInboundMessageParser` for `SourceName = "mqtt"`
- `MqttPayloadSignatureValidator` — "valid by mTLS" no-op validator
- `MqttTopicMapper` — extracts the device serial from a `devices/{serial}/...` topic
- `IoTMqttSettingDefinitionProvider` — registers the per-tenant settings:
  - `IoT:Mqtt:TopicPattern` (default `devices/+/telemetry`)
  - `IoT:Mqtt:CertificateSecretName` (vault key holding the client cert)
  - `IoT:Mqtt:CertificatePassword` (sensitive; encrypted at rest)
  - `IoT:Mqtt:DefaultQoS` (default `1`)
- `NullIoTMqttBridge` — default no-op so the module is safe to register without an
  implementation
- Feature flag — `IoT.MqttBridge` (per tenant via `IFeatureChecker`)

For the concrete MQTTnet v5 implementation with mTLS and Polly-based reconnect,
add **`Granit.IoT.Mqtt.Mqttnet`**.

## Quick start

```csharp
builder.AddModule<GranitIoTMqttModule>();
builder.AddModule<GranitIoTMqttMqttnetModule>(); // from Granit.IoT.Mqtt.Mqttnet
```

Configure (`appsettings.json`):

```json
{
  "IoT": {
    "Mqtt": {
      "BrokerUri": "mqtts://broker.example.com:8883",
      "ClientId": "granit-iot-prod"
    }
  }
}
```

Set the per-tenant settings (`IoT:Mqtt:TopicPattern`,
`IoT:Mqtt:CertificateSecretName`, …) through `Granit.Settings`, store the cert
itself in your configured vault (HashiCorp / Azure Key Vault / AWS Secrets Manager
/ GCP Secret Manager), then enable the feature flag `IoT.MqttBridge` for the
tenants that should accept MQTT ingestion.
