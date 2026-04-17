# Granit.IoT.Aws.Shadow

Bidirectional bridge between the cloud-agnostic
[`Granit.IoT.Aws`](../Granit.IoT.Aws/README.md) `AwsThingBinding` and the
AWS IoT Device Shadow document — AWS's "digital twin" of the device.

```text
┌──────────────────┐  Activated/Suspended/Reactivated  ┌──────────────────┐
│ Device aggregate │ ─────────────────────────────────▶│  reported.{...}  │
└──────────────────┘                                   │   AWS Shadow     │
                                                       │  desired.{...}   │
                                                       └────────▲─────────┘
                                                                │ polled every
                                          DeviceDesiredState    │ 30s (default)
                                          ChangedEvent ◀────────┘
                                          (consumed by PR #6 IoT Jobs)
```

## What ships here

| Layer | File |
| ----- | ---- |
| Abstraction | `IDeviceShadowSyncService` (`PushReportedAsync`, `GetShadowAsync`) |
| Implementation | `DefaultDeviceShadowSyncService` — wraps `IAmazonIotData` |
| Lifecycle bridge | `DeviceLifecycleShadowHandler` — Wolverine handlers on `DeviceActivatedEvent`, `DeviceSuspendedEvent`, `DeviceReactivatedEvent` push `{"status":"…","updatedAt":"…"}` to the reported document |
| Delta poller | `ShadowDeltaPollingService` — `BackgroundService` walking active bindings on `PollIntervalSeconds` (default 30s) |
| Domain event | `DeviceDesiredStateChangedEvent` — emitted via `ILocalEventBus` when the polling service detects a non-empty `delta` block |
| Options | `AwsShadowOptions` (interval, batch size, auto-push toggle) |
| Metrics | `AwsShadowMetrics` — `reported_pushed`, `update_failures`, `delta_detected` |
| Module + DI | `GranitIoTAwsShadowModule`, `AddGranitIoTAwsShadow()` |

## Polling vs event-driven

The polling implementation costs `N / PollBatchSize` Get calls every
`PollIntervalSeconds` and is fine up to a few thousand devices per host.
Larger fleets must switch to the event-driven path:

1. AWS IoT Rule on `$aws/things/+/shadow/update` published to SNS or
   directly to SQS.
2. The host consumes those messages and feeds them straight into the
   existing `IAwsThingBindingReader` + `ILocalEventBus.PublishAsync` flow.

To disable polling but keep the rest of the bridge:

```csharp
services.RemoveAll<IHostedService>();
// or only remove the ShadowDeltaPollingService specifically
```

## Idempotence

- `PushReportedAsync` is naturally idempotent: AWS merges the supplied
  `reported` block over the existing one. Re-delivery from Wolverine
  produces no observable side-effect beyond an extra metric tick.
- `GetShadowAsync` returns `null` when AWS replies with
  `ResourceNotFoundException` — happens for Things that have never
  exchanged a shadow update.
- The polling service swallows per-binding exceptions so one bad shadow
  does not stop the sweep.

## Configuration

```jsonc
{
  "IoT": {
    "Aws": {
      "Shadow": {
        "PollIntervalSeconds": 30,
        "PollBatchSize": 100,
        "AutoPushLifecycleStatus": true
      }
    }
  }
}
```

## What does not land here

- **Consumption** of `DeviceDesiredStateChangedEvent` — that arrives in
  PR #6 (`Granit.IoT.Aws.Jobs`) where the IoT Jobs dispatcher turns each
  delta key into a device-targeted command.
- **Provider-specific shadow names**. AWS supports unnamed and named
  shadows. The current implementation talks to the unnamed (classic)
  shadow only. Named-shadow support is straight-forward and tracked as
  a follow-up.
