# Granit.IoT

IoT & Industry 4.0 integration module family for the
[Granit](https://github.com/granit-fx/granit-dotnet) modular .NET framework.

Granit.IoT enables SaaS applications to manage IoT devices, ingest telemetry,
trigger alerts and workflows, and expose device state to AI agents — all within
Granit's multi-tenant B2B model.

## Architecture

19 focused packages organised in 3 cohesion rings, plus two meta-bundles:

```text
Ring 1 — Device Management
  Granit.IoT                          Abstractions, DDD domain, events
  Granit.IoT.Endpoints                Minimal API route groups
  Granit.IoT.EntityFrameworkCore      Isolated IoTDbContext, migrations
  Granit.IoT.EntityFrameworkCore.Postgres   PostgreSQL provider + BRIN/GIN/jsonb
  Granit.IoT.EntityFrameworkCore.Timescale  TimescaleDB hypertables (opt-in)
  Granit.IoT.BackgroundJobs           Telemetry purge, heartbeat timeout

Ring 2 — Ingestion
  Granit.IoT.Ingestion                Provider-agnostic pipeline
  Granit.IoT.Ingestion.Endpoints      POST /iot/ingest/{source} (202 Accepted)
  Granit.IoT.Ingestion.Scaleway       Scaleway IoT Hub HMAC-SHA256 + MQTT parser
  Granit.IoT.Ingestion.Aws            AWS IoT Core (SNS, Direct, API Gateway)
  Granit.IoT.Mqtt + .Mqttnet          MQTT broker integration
  Granit.IoT.Wolverine                Wolverine handlers, threshold evaluation

Ring 3 — Cross-cutting Bridges
  Granit.IoT.Notifications            Threshold alerts via INotificationPublisher
  Granit.IoT.Timeline                 Device domain events via ITimelineWriter
  Granit.IoT.Mcp                      MCP tool wrappers for device/telemetry
  Granit.IoT.Aws                      AWS bridge — companion AwsThingBinding,
                                      IAwsIoTCredentialProvider
  Granit.IoT.Aws.EntityFrameworkCore  Isolated AwsBindingDbContext (iotaws_*)
  Granit.IoT.Aws.Provisioning         Idempotent saga, X.509 certs, Secrets Manager
  Granit.IoT.Aws.Shadow               Bidirectional Device Shadow sync
  Granit.IoT.Aws.Jobs                 IoT Jobs command dispatcher
  Granit.IoT.Aws.FleetProvisioning    JITP endpoints + claim cert rotation

Meta-packages
  Granit.Bundle.IoT                   Cloud-agnostic core stack
  Granit.Bundle.IoT.Aws               Opt-in full AWS bridge family
```

See [`docs/architecture.md`](docs/architecture.md) for the per-ring
breakdown and [`docs/aws-bridge.md`](docs/aws-bridge.md) for the AWS
bridge in depth.

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
