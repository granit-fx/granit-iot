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

## Enabling PostgreSQL partitioning (recommended for production)

The `iot_telemetry_points` table grows fast (a 100 k-device fleet at 6 publishes/min
produces ~260 M rows/month). RANGE partitioning by `RecordedAt` enables cheap monthly
drops, partition pruning on time-range queries, and isolated index maintenance.

In your application's IoT migration `Up()`:

```csharp
migrationBuilder.EnableTelemetryPartitioning();          // converts the parent table
migrationBuilder.CreateTelemetryPartition(2026, 4);      // current month
migrationBuilder.CreateTelemetryPartition(2026, 5);      // next month
```

Future months are created automatically by `TelemetryPartitionMaintenanceJob`
(in `Granit.IoT.BackgroundJobs`). Without partitioning the maintenance job logs
a single warning and exits cleanly, so partitioning remains opt-in.

> `EnableTelemetryPartitioning()` is designed for an **empty** table. Converting a
> populated table requires a separate data-copy migration that is out of scope
> here. Adopt partitioning at deployment time, before significant ingestion.

## Documentation

See the [full documentation](https://granit-fx.dev).
