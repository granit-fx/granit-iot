# Granit.IoT.Wolverine

Wolverine message handlers for `Granit.IoT`. Handles `TelemetryIngestedEto` from the outbox:
persists the `TelemetryPoint`, updates `Device.LastSeenAt`, evaluates per-tenant thresholds
read from `Granit.Settings`, and emits `TelemetryThresholdExceededEto` if breached — all in
one transactional batch.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT.Wolverine
```

## Dependencies

- `Granit.IoT`
- `Granit.IoT.EntityFrameworkCore`
- `Granit.IoT.Ingestion`
- `Granit.Events.Wolverine`
- `Granit.Settings`
- `Granit.Wolverine`
- `WolverineFx`

## Documentation

See the [full documentation](https://granit-fx.dev).
