# Granit.IoT.Endpoints

Minimal API REST endpoints for `Granit.IoT`. Provides device CRUD management (`/iot/devices`)
with provisioning, update, and decommission workflows, and telemetry query endpoints
(`/iot/telemetry`) with time-range, latest, and server-side aggregation support.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT.Endpoints
```

## Dependencies

- `Granit.IoT`
- `Granit.Authorization`
- `Granit.Guids`
- `Granit.Http.ApiDocumentation`
- `Granit.Timing`
- `Granit.Validation`
- `FluentValidation`

## Documentation

See the [full documentation](https://granit-fx.dev).
