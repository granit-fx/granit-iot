# Granit.IoT.Ingestion

Provider-agnostic telemetry ingestion pipeline for `Granit.IoT`. Validates cryptographic
signatures, parses provider envelopes into a normalized `ParsedTelemetryBatch`, deduplicates
by transport message ID via Redis (5 min TTL), and dispatches `TelemetryIngestedEto` to the
Wolverine outbox. Returns `202 Accepted` immediately — no synchronous DB write.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT.Ingestion
```

## Dependencies

- `Granit.IoT`
- `Granit.Diagnostics`
- `Granit.Http.Idempotency`
- `Granit.Timing`

## Documentation

See the [full documentation](https://granit-fx.dev).
