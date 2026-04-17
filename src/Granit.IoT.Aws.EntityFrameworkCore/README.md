# Granit.IoT.Aws.EntityFrameworkCore

EF Core persistence layer for the [Granit.IoT.Aws](../Granit.IoT.Aws/README.md)
bridge. Hosts the isolated `AwsBindingDbContext` that stores the
`AwsThingBinding` companion of the cloud-agnostic `Device`.

## Why an isolated context

Each Granit module owns its own `DbContext`. The AWS bridge tables
(`iotaws_thing_bindings`) live alongside — but never inside — the core IoT
tables (`iot_devices`, `iot_telemetry_points`). Two consequences:

- A deployment that swaps AWS for another cloud provider drops the
  `iotaws_*` tables without touching the core schema.
- Migrations evolve independently. The AWS bridge can ship a breaking
  schema change without forcing a coordinated migration across all
  IoT modules.

## Indexes

| Index | Purpose |
| ----- | ------- |
| `(tenant_id, device_id)` UNIQUE | Enforces the 1:1 relationship between `Device` and its `AwsThingBinding`. |
| `(thing_name)` UNIQUE | `ThingName` is global on an AWS account. The `t{tenantId:N}-{serial}` format means tenant isolation is also enforced by this constraint. |
| `(tenant_id, provisioning_status)` | Backs the reconciliation queries that surface stuck `Pending` or expired bindings. |

## Provider-agnostic

This package targets generic EF Core. Provider-specific column mappings
(jsonb on PostgreSQL, etc.) live in companion packages — see
`Granit.IoT.Aws.EntityFrameworkCore.Postgres` once it ships.
