# Granit.IoT Documentation

IoT device management, telemetry ingestion, and multi-tenant operations for
.NET 10 SaaS applications built on the
[Granit framework](https://github.com/granit-fx/granit-dotnet).

Granit.IoT ships 13 focused NuGet packages plus a meta-bundle. It gives B2B
SaaS teams a production-ready foundation to:

- Manage the full lifecycle of **IoT devices** with DDD, audit trail, and
  multi-tenant isolation
- **Ingest telemetry** from Scaleway IoT Hub, MQTT brokers (and soon AWS IoT Core)
  with HMAC signature validation, Redis deduplication, and a Wolverine outbox
- Keep time-series data **manageable at scale** via BRIN indexes, JSONB
  metrics, and monthly RANGE partitioning on PostgreSQL
- Plug IoT events into **existing Granit infrastructure** — notifications,
  timeline audit chatter, workflow state transitions — no reinvented wheels

## Start here

| Goal | Page |
| --- | --- |
| Understand the design and why the packages are split this way | [Architecture](architecture.md) |
| Provision a device and receive your first telemetry in 5 minutes | [Getting started](getting-started.md) |
| See every package at a glance | [Bundle](bundle.md) |

## Deep dives by topic

| Topic | What it covers |
| --- | --- |
| [Device management](device-management.md) | `Device` aggregate, value objects, state machine, CQRS reader/writer, CRUD endpoints, EF Core configuration, PostgreSQL schema and indexes |
| [Telemetry ingestion](telemetry-ingestion.md) | Provider-agnostic pipeline, Scaleway IoT Hub provider, HMAC-SHA256, Redis deduplication, Wolverine outbox, threshold evaluation |
| [MQTT](mqtt.md) | MQTT 3.1.1 / 5.0 broker integration via MQTTnet — Mosquitto, EMQX, HiveMQ, self-hosted, or Scaleway IoT Hub in MQTT mode |
| [Operational hardening](operational-hardening.md) | `StaleTelemetryPurgeJob`, `DeviceHeartbeatTimeoutJob`, `TelemetryPartitionMaintenanceJob` — the jobs that keep the system healthy at scale |
| [TimescaleDB backend](timescaledb.md) | Opt-in hypertable + continuous aggregates for billion-row telemetry tables, when and how to adopt |
| [Notifications bridge](notifications-bridge.md) | Threshold alerts and device-offline alerts via `Granit.Notifications`, per-tenant configuration |
| [Timeline bridge](timeline-bridge.md) | Device lifecycle events become audit chatter in `Granit.Timeline` — ISO 27001 asset traceability |
| [MCP bridge](mcp.md) | IoT readers exposed as MCP tools so AI assistants can query the fleet in natural language, tenant-scoped |
| [Bundle](bundle.md) | `Granit.Bundle.IoT` meta-package — one `AddIoT()` call to enable the full stack |

## Provider support matrix

| Provider | Status | Doc |
| --- | --- | --- |
| Scaleway IoT Hub | Available | [Telemetry ingestion → Scaleway](telemetry-ingestion.md#scaleway-iot-hub) |
| MQTT (Mosquitto, EMQX, HiveMQ, custom broker) | Available | [MQTT](mqtt.md) |
| AWS IoT Core (SNS path) | Available (RSA-SHA256 + cert cache + replay dedup); SigV4 in flight | [Telemetry ingestion → AWS](telemetry-ingestion.md#aws-iot-core) |
| Azure IoT Hub | Not planned | Use the generic MQTT bridge |

## Per-package READMEs

Each package has a short README on GitHub describing its purpose, installation,
and direct dependencies. The documentation here links to them.

- [`Granit.IoT`](../src/Granit.IoT/README.md) — domain
- [`Granit.IoT.Endpoints`](../src/Granit.IoT.Endpoints/README.md) — device CRUD API
- [`Granit.IoT.EntityFrameworkCore`](../src/Granit.IoT.EntityFrameworkCore/README.md) — persistence
- [`Granit.IoT.EntityFrameworkCore.Postgres`](../src/Granit.IoT.EntityFrameworkCore.Postgres/README.md) — PostgreSQL migrations
- [`Granit.IoT.EntityFrameworkCore.Timescale`](../src/Granit.IoT.EntityFrameworkCore.Timescale/README.md) — TimescaleDB backend (opt-in)
- [`Granit.IoT.BackgroundJobs`](../src/Granit.IoT.BackgroundJobs/) — purge, heartbeat, partition maintenance
- [`Granit.IoT.Ingestion`](../src/Granit.IoT.Ingestion/README.md) — ingestion pipeline
- [`Granit.IoT.Ingestion.Endpoints`](../src/Granit.IoT.Ingestion.Endpoints/README.md) — webhook endpoint
- [`Granit.IoT.Ingestion.Scaleway`](../src/Granit.IoT.Ingestion.Scaleway/README.md) — Scaleway provider
- [`Granit.IoT.Ingestion.Aws`](../src/Granit.IoT.Ingestion.Aws/README.md) — AWS IoT Core provider (SNS first slice)
- [`Granit.IoT.Wolverine`](../src/Granit.IoT.Wolverine/README.md) — Wolverine handlers
- [`Granit.IoT.Mqtt`](../src/Granit.IoT.Mqtt/README.md) — MQTT abstractions
- [`Granit.IoT.Mqtt.Mqttnet`](../src/Granit.IoT.Mqtt.Mqttnet/README.md) — MQTTnet implementation
- [`Granit.IoT.Notifications`](../src/Granit.IoT.Notifications/README.md) — notifications bridge
- [`Granit.IoT.Timeline`](../src/Granit.IoT.Timeline/) — timeline bridge
- [`Granit.IoT.Mcp`](../src/Granit.IoT.Mcp/README.md) — MCP tools for AI assistants
- [`Granit.Bundle.IoT`](../src/bundles/Granit.Bundle.IoT/README.md) — meta-package

## Compliance

- **GDPR** — per-tenant retention, right to erasure via bulk delete and
  partition drops, no telemetry leaves the host database
- **ISO 27001** — full audit trail on `Device` (`FullAuditedAggregateRoot`
  plus `Granit.Timeline` system-log entries), encrypted credentials at rest

## Project status

Granit.IoT is **pre-release** — actively developed, not yet tagged `v1.0.0`.
Phase 1 MVP (device management + Scaleway ingestion + notifications/timeline
bridges) is complete. Phase 2 (MQTT, operational hardening) is complete.
Phase 3 (AWS, AI/MCP, TimescaleDB) is in design.

Track progress on the
[Epic #1 — IoT & Industry 4.0 Integration Module Family](https://github.com/granit-fx/granit-iot/issues/1).
