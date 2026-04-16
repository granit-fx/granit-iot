# Granit.IoT.EntityFrameworkCore.Postgres

PostgreSQL-specific optimizations for `Granit.IoT`. Applies `jsonb` column type for telemetry
metrics and device tags, and provides migration helpers for BRIN index on `recorded_at` and
GIN index on metrics for efficient time-range scans and containment queries.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT.EntityFrameworkCore.Postgres
```

## Dependencies

- `Granit.IoT.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`

## Documentation

See the [full documentation](https://granit-fx.dev).
