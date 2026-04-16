# Granit.Bundle.IoT

Meta-package grouping the Phase-1 Granit.IoT modules so an application gets the
full IoT stack with one NuGet reference and one builder call.

Part of the [granit](https://granit-fx.dev) framework.

## Included packages

| Package | Role |
| --- | --- |
| `Granit.IoT` | Domain model (Device, TelemetryPoint, value objects), abstractions, diagnostics |
| `Granit.IoT.EntityFrameworkCore` | EF Core persistence, IoTDbContext, configurations |
| `Granit.IoT.EntityFrameworkCore.Postgres` | PostgreSQL-specific model conventions + migrations (BRIN, GIN, JSONB) |
| `Granit.IoT.Endpoints` | Minimal API: device CRUD + telemetry queries |
| `Granit.IoT.Ingestion` | Provider-agnostic ingestion pipeline (signature, parsing, deduplication) |
| `Granit.IoT.Ingestion.Endpoints` | Webhook endpoint `POST /iot/ingest/{source}` |
| `Granit.IoT.Ingestion.Scaleway` | Scaleway IoT Hub provider (HMAC-SHA256, JSON parser) |
| `Granit.IoT.Wolverine` | Wolverine handlers: telemetry persistence, threshold evaluation |
| `Granit.IoT.Notifications` | Bridge to Granit.Notifications: threshold alerts, device-offline alerts |

## Installation

```bash
dotnet add package Granit.Bundle.IoT
```

## Usage

```csharp
builder.Services
    .AddGranit(builder.Configuration)
    .AddIoT();
```

This registers all nine modules in topological order. Each module's own
`[DependsOn]` graph still drives the actual DI initialization order, so the
bundle's `AddModule<T>()` calls are simply a complete enumeration — there is no
hidden ordering risk.

## Documentation

See the [granit-iot repository](https://github.com/granit-fx/granit-iot).
