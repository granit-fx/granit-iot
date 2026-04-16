# Granit.IoT.EntityFrameworkCore

EF Core persistence layer for `Granit.IoT`. Provides isolated `IoTDbContext` with time-series
optimized schema, CQRS reader/writer implementations via `EfStoreBase`, and entity configurations
for `Device` and `TelemetryPoint` with multi-tenant query filters.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.IoT.EntityFrameworkCore
```

## Dependencies

- `Granit.IoT`
- `Granit.MultiTenancy`
- `Granit.Persistence`
- `Granit.Persistence.EntityFrameworkCore`

## Documentation

See the [full documentation](https://granit-fx.dev).
