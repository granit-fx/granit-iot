# Granit.IoT.Ingestion.Endpoints

HTTP webhook endpoint for `Granit.IoT.Ingestion`. Exposes `POST /iot/ingest/{source}` with
tenant-partitioned rate limiting, raw-body capture for signature verification, and
`202 Accepted` semantics so IoT hubs (Scaleway, AWS IoT, Azure IoT Hub) never time out.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT.Ingestion.Endpoints
```

## Dependencies

- `Granit.IoT.Ingestion`
- `Granit.Http.ApiDocumentation`
- `Granit.RateLimiting`
- `Granit.Validation`

## Documentation

See the [full documentation](https://granit-fx.dev).
