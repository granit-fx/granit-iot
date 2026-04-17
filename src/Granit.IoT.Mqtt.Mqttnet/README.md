# Granit.IoT.Mqtt.Mqttnet

**Why** — `Granit.IoT.Mqtt` declares the abstraction. This package plugs in a concrete
broker client (MQTTnet v5) so direct-MQTT deployments can reuse the IoT ingestion
pipeline without dragging the MQTTnet dependency into apps that only use HTTP webhooks.

## What it does

- **mTLS broker connection** — client certificate fetched from `Granit.Vault` via
  `ISecretStore`, no plaintext MQTT (`mqtts://` enforced at startup).
- **Polly v8 reconnect** — exponential backoff with jitter, max delay 30 s, retries
  forever until the host shuts down. Keeps the bridge alive across network blips,
  broker restarts, and credential rotations.
- **Proactive certificate reload** — when the loaded `SecretDescriptor.ExpiresOn`
  (or the X509 `NotAfter` fallback) is within the configured warning window
  (`IoT:Mqtt:CertificateExpiryWarningMinutes`, default 5 min), the bridge re-fetches
  the cert and gracefully reconnects. No reliance on MQTTnet to surface TLS
  handshake failures via `DisconnectedAsync`.
- **Local feature-flag cache** — `IFeatureChecker.IsEnabledAsync("IoT.MqttBridge")`
  is wrapped in a TTL snapshot (`IoT:Mqtt:FeatureFlagCacheSeconds`, default 30 s)
  to remove `Task` allocations from the per-message hot loop.
- **Pure transport adapter** — wraps each MQTT message in a JSON envelope and feeds
  it to `IIngestionPipeline.ProcessAsync("mqtt", ...)`. Parsing, deduplication,
  device resolution and outbox publishing are unchanged from the HTTP-webhook path.

## Quick start

```csharp
builder.AddModule<GranitIoTMqttModule>();
builder.AddModule<GranitIoTMqttMqttnetModule>();
// Pick one:
builder.AddModule<GranitVaultHashiCorpModule>();
// builder.AddModule<GranitVaultAzureModule>();
// builder.AddModule<GranitVaultAwsModule>();
// builder.AddModule<GranitVaultGoogleCloudModule>();
```

```json
{
  "IoT": {
    "Mqtt": {
      "BrokerUri": "mqtts://broker.example.com:8883",
      "ClientId": "granit-iot-prod",
      "DefaultQoS": 1,
      "FeatureFlagCacheSeconds": 30,
      "CertificateExpiryWarningMinutes": 5
    }
  }
}
```

Per-tenant or global settings (via `Granit.Settings`):

| Key | Default | Notes |
|-----|---------|-------|
| `IoT:Mqtt:TopicPattern` | `devices/+/telemetry` | MQTT subscription pattern |
| `IoT:Mqtt:CertificateSecretName` | _required_ | Vault key holding the PFX/PEM bytes |
| `IoT:Mqtt:CertificatePassword` | `null` | Sensitive — encrypted at rest |
| `IoT:Mqtt:DefaultQoS` | `1` | 0/1/2 |

The feature flag `IoT.MqttBridge` (`IFeatureChecker`) gates the bridge — disabled by
default. Flip it on per environment / tenant once mTLS is wired up.
