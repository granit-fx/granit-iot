# Granit.Bundle.IoT.Aws

Opt-in meta-package grouping the full Granit.IoT.Aws bridge family. Add this
to a Granit IoT host that runs against AWS IoT Core. Sister bundles (Azure,
Scaleway) ship separately.

Part of the [granit](https://granit-fx.dev) framework.

## Included packages

| Package | Role |
| --- | --- |
| `Granit.IoT.Aws` | Companion `AwsThingBinding`, abstractions, options |
| `Granit.IoT.Aws.EntityFrameworkCore` | EF Core persistence for AWS thing bindings and credentials |
| `Granit.IoT.Aws.Provisioning` | Idempotent provisioning saga (Thing + Certificate + Policy) |
| `Granit.IoT.Aws.Shadow` | Bidirectional Device Shadow synchronization |
| `Granit.IoT.Aws.Jobs` | IoT Jobs command dispatcher |
| `Granit.IoT.Aws.FleetProvisioning` | Just-in-time provisioning (JITP) endpoints |

## Installation

```bash
dotnet add package Granit.Bundle.IoT.Aws
```

## Usage

Add the bundle alongside the core IoT bundle:

```csharp
builder.Services
    .AddGranit(builder.Configuration)
    .AddIoT()
    .AddIoTAws();
```

This registers the AWS bridge modules in topological order. Each module's own
`[DependsOn]` graph still drives the actual DI initialization order, so the
bundle's `AddModule<T>()` calls are simply a complete enumeration — there is
no hidden ordering risk.

## Documentation

See the [granit-iot repository](https://github.com/granit-fx/granit-iot).
