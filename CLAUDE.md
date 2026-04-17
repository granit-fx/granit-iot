# Granit.IoT — Claude Code context

## Project

- **Type**: IoT module family — 15 NuGet packages (3 cohesion rings) plus a meta-bundle
- **Repo**: `granit-fx/granit-iot` (GitHub, open-source, Apache-2.0)
- **Framework dependency**: Published `Granit.*` packages from `granit-dotnet` (via GitHub Packages)
- **Compliance**: GDPR + ISO 27001 (audit trail, tenant isolation, encrypted credentials)

## Stack

.NET 10 | C# 14 | EF Core 10 | Wolverine 5.31+ | PostgreSQL | Redis

## Architecture

```text
src/
  Granit.IoT/                              # Ring 1: abstractions, DDD domain, events
  Granit.IoT.Endpoints/                    # Ring 1: Minimal API route groups
  Granit.IoT.EntityFrameworkCore/          # Ring 1: isolated IoTDbContext, migrations
  Granit.IoT.BackgroundJobs/               # Ring 1: telemetry purge, heartbeat timeout
  Granit.IoT.Ingestion/                    # Ring 2: provider-agnostic pipeline
  Granit.IoT.Ingestion.Endpoints/          # Ring 2: POST /iot/ingest/{source}
  Granit.IoT.Ingestion.Scaleway/           # Ring 2: Scaleway IoT Hub parser
  Granit.IoT.Wolverine/                    # Ring 2: message handlers, threshold eval
  Granit.IoT.Notifications/               # Ring 3: threshold → INotificationPublisher
  Granit.IoT.Timeline/                     # Ring 3: device events → ITimelineWriter
  Granit.IoT.AI.Mcp/                       # Ring 3: [McpServerTool] wrappers
  bundles/
    Granit.Bundle.IoT/                     # Ring 3: meta-package

tests/
  Granit.IoT.Tests/                        # Unit tests (xUnit + Shouldly)
  Granit.IoT.Tests.Integration/            # Integration tests (Testcontainers)
  Granit.IoT.ArchitectureTests/            # Cross-cutting architecture rules
```

**Convention**: One project = one NuGet package. Namespace = project name.

## Key design decisions

- **JSONB telemetry model** — 1 row per device payload (not EAV), GIN index on `Metrics`
- **202 Accepted + Wolverine outbox** — ingestion never blocks the HTTP response
- **Transport-level deduplication** — Redis key on message ID (TTL 5 min)
- **Multi-tenancy from day 1** — every entity implements `IMultiTenant`
- **PostgreSQL-native time-series** — BRIN index on `recorded_at`, monthly partitioning
- **No separate RulesEngine** — `Granit.Workflow` handles device state transitions
- **No separate Notifications** — thin bridge to `Granit.Notifications`

## Commands

```bash
dotnet build Granit.IoT.slnx              # Build all
dotnet test Granit.IoT.slnx               # Run all tests
dotnet format Granit.IoT.slnx             # Format check
```

## Module conventions (from granit-dotnet)

- Domain types in `Granit.IoT` (abstractions project)
- EF configurations in `Granit.IoT.EntityFrameworkCore`
- Endpoints in `Granit.IoT.Endpoints` (Minimal API route groups)
- Wolverine handlers in `Granit.IoT.Wolverine`
- DDD: `FullAuditedAggregateRoot` for `Device`, value objects for credentials
- CQRS: separate read/write paths via Wolverine
- Named query filters for multi-tenancy (`ApplyGranitConventions`)
- Permission definitions per module (`IoTPermissions` static class)

## Phased delivery

- **Phase 1 (MVP)**: #2 Core Device Management, #3 Telemetry Ingestion, #4 Cross-cutting Bridges
- **Phase 2**: #5 MQTT Bridge, #6 Operational Hardening
- **Phase 3**: #7 AI/MCP Integration, #8 TimescaleDB Support
