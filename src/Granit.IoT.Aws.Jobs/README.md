# Granit.IoT.Aws.Jobs

AWS IoT Jobs command dispatcher for the Granit IoT bridge. Translates
Granit device commands into persistent AWS Jobs, tracks their execution,
and surfaces completion as `DeviceCommandCompletedEvent` /
`DeviceCommandFailedEvent`. Also closes the loop opened by PR #5 by
consuming `DeviceDesiredStateChangedEvent` and dispatching one job per
shadow delta.

## Why IoT Jobs (not MQTT pub/sub)

The IoT Jobs service persists each command server-side. A device that is
offline at dispatch time receives the job on its next reconnect — MQTT
pub/sub drops messages issued while the device is unreachable. For
commands that must reach a device (firmware update, configuration push,
reboot), Jobs is the right primitive.

## Targeting

Three modes, all idempotent:

| Mode | When to use | Resolution |
| ---- | ----------- | ---------- |
| `Thing` | Single device | `DeviceCommandTarget.ForThing(binding.ThingArn)` |
| `ThingGroup` | Static group (created out-of-band) | `DeviceCommandTarget.ForGroup(arn)` |
| `DynamicThingGroup` | Query-based fleet (e.g. `attributes.model:THERM-PRO`) | Dispatcher reuses or creates `granit-dynamic-{sha256(query)[:16]}` |

The dispatcher names dynamic groups deterministically — the same query
always lands in the same group, so re-dispatch reuses the existing AWS
resource.

## Idempotence

- `DispatchAsync` short-circuits on the tracking store before any AWS
  call: if `correlationId` is already known, the existing `jobId` is
  returned without touching IoT.
- A `ResourceAlreadyExistsException` from `CreateJob` is treated as a
  benign idempotent reuse (lost cache entry / host restart scenario).
- The shadow-delta consumer derives its `correlationId` from
  `(deviceId, shadowVersion)` via SHA-256 — the same delta always maps
  to the same correlation, so re-delivery dispatches into the same job.

## Tracking store

The default `InMemoryJobTrackingStore` is per-host. **Production
deployments running more than one host must replace it** so a job
dispatched on host A is observed by the poller on host B:

```csharp
services.AddSingleton<IJobTrackingStore, RedisJobTrackingStore>();
```

A reference Redis implementation is straightforward (a hash per
`correlationId` with `EXPIRE` set to `JobTrackingTtlHours`).

## Polling vs notification

`IoTJobStatusPollingService` polls each tracked job every
`StatusPollIntervalSeconds` (default 300s). Production deployments with
strict latency budgets should switch to the IoT Rule path:

1. AWS IoT Rule on `$aws/events/jobExecution/+/+` published to SQS / SNS.
2. The host consumes those messages and feeds them straight into the
   same `ILocalEventBus.PublishAsync` flow the polling service uses.

The polling service detects terminal statuses (`SUCCEEDED`, `FAILED`,
`REJECTED`, `TIMED_OUT`, `CANCELED`, `REMOVED`) and removes the tracking
entry on terminal status — successful jobs are not re-inspected.

## Configuration

```jsonc
{
  "IoT": {
    "Aws": {
      "Jobs": {
        "JobIdPrefix": "granit",
        "JobTrackingTtlHours": 72,
        "StatusPollIntervalSeconds": 300,
        "StatusPollBatchSize": 100
      }
    }
  }
}
```

## Metrics

| Counter | When it ticks |
| ------- | ------------- |
| `granit.iot.aws.jobs.dispatched` | A new IoT Job was created. |
| `granit.iot.aws.jobs.idempotent_reuse` | Re-dispatch reused an existing job (cache hit or AWS `ResourceAlreadyExists`). |
| `granit.iot.aws.jobs.completed` | Polling surfaced a terminal `SUCCEEDED`. |
| `granit.iot.aws.jobs.failed` | Polling surfaced a terminal failure status. |
| `granit.iot.aws.jobs.dispatch_errors` | `CreateJob` failed for reasons other than idempotency. |

All counters are tagged with `tenant_id` and `operation`.

## What does not land here

- A Redis-backed `IJobTrackingStore` — single-host MVP only.
- The IoT Rule → SQS event-driven path; documented above as the
  production upgrade.
- Custom command schemas — consumers ship their own `IDeviceCommand`
  records; this package only ships the shadow-delta consumer.
