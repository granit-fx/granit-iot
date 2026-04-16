# Granit.IoT

IoT device management abstractions, DDD domain model, and events. Provides `Device` aggregate root
with full lifecycle state machine (Provisioning, Active, Suspended, Decommissioned), `TelemetryPoint`
append-only entity with JSONB metrics, CQRS reader/writer interfaces, and OpenTelemetry diagnostics.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT
```

## Dependencies

- `Granit`
- `Granit.Diagnostics`
- `Granit.Encryption`
- `Granit.Timeline`
- `Granit.Workflow`

## Documentation

See the [full documentation](https://granit-fx.dev).
