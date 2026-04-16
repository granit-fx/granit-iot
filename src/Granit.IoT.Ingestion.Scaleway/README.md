# Granit.IoT.Ingestion.Scaleway

Scaleway IoT Hub provider for `Granit.IoT.Ingestion`. Provides HMAC-SHA256 webhook signature
validation (`X-Scaleway-Signature`) and a JSON envelope parser that decodes Base64 payloads
and extracts device serial numbers from configurable MQTT topic patterns.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT.Ingestion.Scaleway
```

## Dependencies

- `Granit.IoT.Ingestion`

## Documentation

See the [full documentation](https://granit-fx.dev).
