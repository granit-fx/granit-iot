# Granit.IoT.Timeline

Bridge package: writes `Granit.Timeline` entries for device lifecycle events
(provisioned, activated, suspended, reactivated, decommissioned).

Without this package the IoT module raises domain events but no historical
trace is persisted on the affected `Device`. This package subscribes to those
events and turns them into timeline entries, queryable from the device
detail screen and any audit tooling.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT.Timeline
```

## Dependencies

- `Granit`
- `Granit.Timeline`
- `Granit.IoT`

## Documentation

See the [granit-iot repository](https://github.com/granit-fx/granit-iot).
