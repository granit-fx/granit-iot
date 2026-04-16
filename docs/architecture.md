# Architecture & Design Decisions

## Why Granit.IoT?

SaaS platforms built on Granit need a standardized way to manage IoT devices,
ingest telemetry data, trigger alerts, and expose device state to AI agents.
Granit.IoT provides this as a module family that integrates seamlessly with
Granit's multi-tenant B2B model.

## Package Rings

The 12 packages are organized in 3 cohesion rings, where each ring depends
only on itself and inner rings:

```text
┌─────────────────────────────────────────────────────┐
│ Ring 3 — Cross-cutting Bridges                      │
│  Notifications · Timeline · AI.Mcp · Bundle.IoT    │
├─────────────────────────────────────────────────────┤
│ Ring 2 — Ingestion                                  │
│  Ingestion · Ingestion.Endpoints ·                  │
│  Ingestion.Scaleway · Wolverine                     │
├─────────────────────────────────────────────────────┤
│ Ring 1 — Device Management                          │
│  IoT · Endpoints · EntityFrameworkCore ·            │
│  BackgroundJobs                                     │
└─────────────────────────────────────────────────────┘
```

## Key Design Decisions

### JSONB Telemetry Model (not EAV)

Each `TelemetryPoint` stores the full device payload as a single JSONB column
(`Metrics`). This avoids the classic Entity-Attribute-Value antipattern and
enables GIN-indexed queries on arbitrary metric keys.

**Trade-off**: less normalized, but dramatically simpler queries and better
write throughput for high-frequency telemetry.

### 202 Accepted + Wolverine Outbox

Ingestion endpoints return `202 Accepted` immediately and publish messages
via Wolverine's transactional outbox. This decouples HTTP response time from
processing latency and guarantees at-least-once delivery.

### Transport-Level Deduplication

A Redis key based on the transport message ID (e.g., `X-Scaleway-Message-Id`)
with a 5-minute TTL prevents duplicate processing. This is intentionally at
the transport level, not the business level, to keep the pipeline generic.

### PostgreSQL-Native Time-Series

Instead of introducing TimescaleDB from day one:

- **Phase 1-2**: BRIN index on `recorded_at`, monthly table partitioning
- **Phase 3**: Optional TimescaleDB hypertable migration

This keeps the operational complexity low during MVP while leaving a clear
upgrade path.

### Multi-Tenancy from Day One

Every `Device` and `TelemetryPoint` implements `IMultiTenant`. Query filters
are enforced via `ApplyGranitConventions`, preventing cross-tenant data access
at the EF Core level.

### No Separate Rules Engine

Device state transitions (online/offline/alert) use `Granit.Workflow` and
`IWorkflowStateful`. Threshold evaluation happens in Wolverine handlers,
not in a separate rules engine package.

### No Separate Notifications Module

`Granit.IoT.Notifications` is a thin bridge that maps
`TelemetryThresholdExceededEto` to `INotificationPublisher`. The actual
notification infrastructure (email, SMS, SSE) comes from `Granit.Notifications`.

## Security Model

- **Inbound webhooks**: HMAC-SHA256 signature validation before any DB access
- **Rate limiting**: per-tenant rate limits via `Granit.RateLimiting`
- **Device credentials**: encrypted at rest via `Granit.Encryption`, never
  returned in API responses (`[SensitiveData]` attribute)
- **Audit trail**: `FullAuditedAggregateRoot` for all `Device` entities
  (CreatedBy, ModifiedBy, DeletedBy — ISO 27001 asset traceability)

## GDPR Compliance

- Strict `TenantId` isolation on all entities
- Per-tenant configurable retention via `Granit.Settings` (`IoT:TelemetryRetentionDays`)
- `StaleTelemetryPurgeJob` bulk-deletes by `(tenant_id, recorded_at)` index
