# Granit.IoT

IoT & Industry 4.0 integration module family for the
[Granit](https://github.com/granit-fx/granit-dotnet) modular .NET framework.

Granit.IoT enables SaaS applications to manage IoT devices, ingest telemetry,
trigger alerts and workflows, and expose device state to AI agents — all within
Granit's multi-tenant B2B model.

## Architecture

12 packages organized in 3 cohesion rings:

```text
Ring 1 — Device Management
  Granit.IoT                          Abstractions, DDD domain, events
  Granit.IoT.Endpoints                Minimal API route groups
  Granit.IoT.EntityFrameworkCore      Isolated IoTDbContext, migrations
  Granit.IoT.BackgroundJobs           Telemetry purge, heartbeat timeout

Ring 2 — Ingestion
  Granit.IoT.Ingestion                Provider-agnostic pipeline
  Granit.IoT.Ingestion.Endpoints      POST /iot/ingest/{source} (202 Accepted)
  Granit.IoT.Ingestion.Scaleway       Scaleway IoT Hub HMAC-SHA256 + MQTT parser
  Granit.IoT.Wolverine                Wolverine handlers, threshold evaluation

Ring 3 — Cross-cutting Bridges
  Granit.IoT.Notifications            Threshold alerts via INotificationPublisher
  Granit.IoT.Timeline                 Device domain events via ITimelineWriter
  Granit.IoT.AI.Mcp                   MCP tool wrappers for device/telemetry
  Granit.Bundle.IoT                   Meta-package
```

## Getting started

```bash
dotnet restore
dotnet build
dotnet test
```

## Running tests

```bash
dotnet test
```

## License

Apache-2.0
