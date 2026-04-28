# Granit.IoT.BackgroundJobs

Recurring background jobs for the Granit.IoT module: stale telemetry purge
(GDPR retention), device heartbeat timeout detection, and PostgreSQL
partition maintenance.

Part of the [granit](https://granit-fx.dev) framework.

## What it ships

- `TelemetryRetentionPurgeJob` — drops telemetry rows older than the
  per-tenant `IoT:TelemetryRetentionDays` setting (GDPR minimization)
- `DeviceHeartbeatTimeoutService` — flags devices that stop reporting for
  longer than their configured grace window and raises
  `DeviceWentOfflineEto`
- `TelemetryPartitionMaintenanceJob` — creates next month's partition on
  PostgreSQL-partitioned deployments and drops partitions outside the
  retention window
- `DeviceOfflineTrackerCache` — Redis-backed throttle to avoid duplicate
  offline events when the timeout job runs every minute

## Installation

```bash
dotnet add package Granit.IoT.BackgroundJobs
```

## Dependencies

- `Granit`
- `Granit.BackgroundJobs`
- `Granit.MultiTenancy`
- `Granit.Settings`
- `Granit.IoT`
- `Granit.IoT.Wolverine`

## Documentation

See the [granit-iot repository](https://github.com/granit-fx/granit-iot).
