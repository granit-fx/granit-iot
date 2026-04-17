# Granit.IoT.EntityFrameworkCore.Timescale

Opt-in TimescaleDB backend for Granit.IoT telemetry.

Without this package, telemetry queries that span days or weeks scan the raw
`iot_telemetry_points` table — fast enough up to tens of millions of rows, but
painful at billion-row scale. This package converts the table to a TimescaleDB
hypertable, installs hourly and daily continuous aggregates, and replaces
`ITelemetryReader` so `GetAggregateAsync` routes to the pre-materialized
rollup that matches the requested time window.

The package is **not** loaded by `Granit.Bundle.IoT`. Teams that stay on
vanilla PostgreSQL are unaffected — adding the module is a conscious choice
tied to the operational cost of installing the `timescaledb` extension.

## What it ships

- `GranitIoTTimescaleModule` — runs hypertable conversion + continuous
  aggregate DDL on `OnApplicationInitializationAsync`, guarded by an extension
  detection query (warns and skips when `timescaledb` is not installed)
- `TimescaleTelemetryEfCoreReader` — extends `PostgresTelemetryEfCoreReader`:
  - Windows ≥ 24 h → `iot_telemetry_daily` continuous aggregate
  - Windows ≥ 1 h → `iot_telemetry_hourly` continuous aggregate
  - Sub-hourly → raw hypertable via the inherited JSONB path
- `TimescaleSqlBuilder` (internal) — idempotent DDL templates
- `IoTTimescaleMigrationExtensions` — `EnableTelemetryHypertable()`,
  `CreateTelemetryHourlyAggregate()`, `CreateTelemetryDailyAggregate()` for
  teams who prefer migration-driven conversion
- `IoTTimescaleServiceCollectionExtensions.AddGranitIoTTimescale()` — swaps
  the registered `ITelemetryReader`

## Setup

```csharp
builder.Services
    .AddGranit(builder.Configuration)
    .AddModule<GranitIoTModule>()
    .AddModule<GranitIoTEntityFrameworkCoreModule>()
    .AddModule<GranitIoTTimescaleModule>();
```

On first startup the module executes:

```sql
SELECT create_hypertable('iot_telemetry_points', 'RecordedAt',
    chunk_time_interval => INTERVAL '7 days',
    if_not_exists => TRUE, migrate_data => TRUE);

CREATE MATERIALIZED VIEW IF NOT EXISTS iot_telemetry_hourly
WITH (timescaledb.continuous) AS ... WITH NO DATA;

SELECT add_continuous_aggregate_policy('iot_telemetry_hourly',
    start_offset => INTERVAL '3 hours',
    end_offset   => INTERVAL '1 hour',
    schedule_interval => INTERVAL '30 minutes');

-- same for iot_telemetry_daily
```

All DDL is idempotent. Safe to run on empty or populated tables, safe to
re-run on every deploy.

> [!IMPORTANT]
> The `timescaledb` extension must be installed on the database before the
> module runs — `CREATE EXTENSION timescaledb;` (requires superuser). When the
> extension is absent, the module logs a warning and skips DDL. The
> application starts normally but `ITelemetryReader` will fail aggregate
> queries because it expects the continuous aggregate views to exist.

## Continuous aggregate shape

Both `iot_telemetry_hourly` and `iot_telemetry_daily` materialize one row per
`(bucket, DeviceId, TenantId, MetricName)` tuple with pre-computed
`avg_value`, `min_value`, `max_value`, and `count`. The JSONB `Metrics`
column is unpacked via `LATERAL jsonb_each(...)` and filtered to numeric
values only (non-numeric metrics are skipped silently — they are invalid
telemetry anyway).

Result shape:

| Column | Type | Meaning |
| --- | --- | --- |
| `bucket` | `timestamptz` | Start of the bucket (`time_bucket` output) |
| `DeviceId` | `uuid` | Device identifier |
| `TenantId` | `uuid` | Tenant identifier (for query-filter parity) |
| `MetricName` | `text` | JSONB key from the raw `Metrics` dictionary |
| `avg_value` | `double precision` | Bucketed average |
| `min_value` | `double precision` | Bucketed min |
| `max_value` | `double precision` | Bucketed max |
| `count` | `bigint` | Number of raw rows in the bucket for this metric |

`TimescaleTelemetryEfCoreReader` aggregates across buckets with a count-weighted
average for `Avg`, naïve `MIN`/`MAX` for `Min`/`Max`, and `SUM(count)` for `Count`.

## Anti-patterns to avoid

> [!WARNING]
> **Don't register both `Granit.IoT.EntityFrameworkCore.Postgres` and
> `Granit.IoT.EntityFrameworkCore.Timescale` expecting different readers.**
> Timescale extends the Postgres reader and replaces the `ITelemetryReader`
> registration. Register both at most once; the Timescale module depends on
> the EF Core base module but not on the Postgres module.

> [!CAUTION]
> **Don't attempt to `CREATE EXTENSION timescaledb` from the application.**
> The extension requires superuser privileges and is typically installed by
> the DBA or managed service at cluster provisioning time. This module only
> *detects* the extension; it never tries to create it.

## See also

- [`Granit.IoT.EntityFrameworkCore`](../Granit.IoT.EntityFrameworkCore/README.md) — base EF Core layer
- [`Granit.IoT.EntityFrameworkCore.Postgres`](../Granit.IoT.EntityFrameworkCore.Postgres/README.md) — PostgreSQL partitioning + JSONB aggregation (parent reader)
- [architecture](../../docs/architecture.md) — ring structure and design decisions
- [timescaledb.md](../../docs/timescaledb.md) — end-to-end guide, when to adopt, performance characteristics
