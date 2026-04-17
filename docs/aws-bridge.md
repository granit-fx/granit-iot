# AWS IoT Core bridge

Provider-specific bridge that maps the cloud-agnostic Granit `Device`
aggregate to AWS IoT Core resources (Things, X.509 certificates, Secrets
Manager entries, Device Shadow documents, IoT Jobs). Six packages, all
**Ring 3** — they react to events raised by the core IoT module without
ever modifying it.

## Why a bridge, not a replacement aggregate

The cloud-agnostic `Device` already owns SerialNumber, Status,
Credential, Heartbeat, Workflow state and Timeline entries. Stamping a
second AWS-specific `Device` next to it would create two sources of
truth and force a permanent synchronisation problem.

We instead introduce a 1:1 companion **`AwsThingBinding`** keyed by
`Device.Id`. The binding stores everything AWS needs (`ThingName`,
`ThingArn`, `CertificateArn`, `CertificateSecretArn`,
`ProvisioningStatus`, `LastShadowReportedAt`,
`ClaimCertificateExpiresAt`, `ProvisionedViaJitp`) — and nothing the
core domain needs.

Reactions to `DeviceProvisionedEvent` / `DeviceActivatedEvent` /
`DeviceDecommissionedEvent` happen via Wolverine handlers in the bridge
packages. The same pattern as the existing
[Notifications](notifications-bridge.md) and
[Timeline](timeline-bridge.md) bridges, just sized for an entire cloud
provider.

## Package map

```text
Granit.IoT (Ring 1)            ───► (cloud-agnostic Device + events)
   ▲
   │ depends on
   │
Granit.IoT.Aws                       ──► companion AwsThingBinding,
                                          ThingName VO, IAwsIoTCredentialProvider
   │
   ├── Granit.IoT.Aws.EntityFrameworkCore   isolated AwsBindingDbContext (iotaws_*)
   │
   ├── Granit.IoT.Aws.Provisioning          IAmazonIoT + IAmazonSecretsManager,
   │                                        idempotent saga handler
   │
   ├── Granit.IoT.Aws.Shadow                IAmazonIotData, reported push +
   │                                        delta polling → DeviceDesiredStateChangedEvent
   │
   ├── Granit.IoT.Aws.Jobs                  IDeviceCommandDispatcher backed by IoT Jobs,
   │                                        deterministic correlation, status polling
   │
   └── Granit.IoT.Aws.FleetProvisioning     POST /verify + /registered (JITP),
                                            ClaimCertificateRotationCheckService
```

The umbrella **`Granit.Bundle.IoT.Aws`** meta-package references all six.
Hosts targeting AWS pull it once; sister bundles for other providers
ship separately and never collide.

## The provisioning saga

`AwsThingBinding.ProvisioningStatus` is the saga state machine:

```text
Pending → ThingCreated → CertIssued → SecretStored → Active
                                                         ▼
                                                Decommissioned
                                          (or Failed for manual reconciliation)
```

`AwsThingBridgeHandler` reacts to `DeviceProvisionedEvent` and walks the
binding through every checkpoint. Each forward step:

1. Short-circuits if the binding has already crossed the matching
   checkpoint (lock-free read of `ProvisioningStatus`).
2. Defensively calls AWS `Describe*` before any `Create*`. A crash
   between an AWS-side success and the matching DB commit recovers
   cleanly on Wolverine replay.
3. Mutates the in-memory binding and lets the handler persist the new
   state immediately afterwards.

Cert + secret are deliberately **fused** into a single saga step:
`CreateKeysAndCertificate` returns a private key that AWS will never
give back, so we persist the key to Secrets Manager (with a
`ClientRequestToken = binding.Id` for native idempotency) inside the
same logical operation as the cert creation. A crash strictly between
the two AWS calls leaves a dangling certificate, surfaced via the
`granit.iot.aws.provisioning.failed` counter and a `Status=Failed`
binding for operator triage.

## ThingName format

`AwsThingBinding.ThingName` is imposed as `t{tenantId:N}-{serialNumber}`
(32-char hex tenant id + dash + serial). Two reasons:

- **Zero collision** across tenants — Guid uniqueness baked in.
- **IAM policy isolation** — AWS IoT policies can use
  `${iot:Connection.Thing.ThingName}` with a strict `t{tenantId}-*`
  prefix to enforce per-tenant scoping at the broker level. The unique
  database constraint on `thing_name` therefore also enforces tenant
  isolation in the persistence layer.

## Credential pipeline

`Granit.IoT.Aws` ships two `IAwsIoTCredentialProvider` implementations:

- **IAM-role mode** — picked when `FleetCredentialSecretArn` is `null`.
  Returns `null` for every key; the AWS SDK default credential chain
  (instance role, ECS task role, env vars) authenticates outbound
  traffic. `IsReady` is always `true`.
- **Rotating mode** — `BackgroundService` that polls an
  `IAwsIoTCredentialLoader` on `RotationCheckIntervalMinutes` (default 5).
  Lock-free volatile reads, stale-ok on refresh failure, bounded initial
  fetch via `TimeProvider`. The Secrets-Manager–backed loader ships in
  `Granit.IoT.Aws.Provisioning` so `Granit.IoT.Aws` itself never
  references the AWS SDK.

The `IsReady` gate must be checked by every endpoint that calls AWS;
hosts publish a matching readiness probe via `HealthChecks`.

## Bidirectional Device Shadow sync

`Granit.IoT.Aws.Shadow` mirrors device lifecycle into the AWS Device
Shadow:

- **Granit → AWS**: `DeviceLifecycleShadowHandler` reacts to
  `Device{Activated,Suspended,Reactivated}Event` and pushes
  `{"status":"…","updatedAt":"…"}` (`IClock` injected for deterministic
  timestamps).
- **AWS → Granit**: `ShadowDeltaPollingService` walks active bindings
  every `PollIntervalSeconds` (default 30s), parses `state.delta`, and
  publishes `DeviceDesiredStateChangedEvent` on `ILocalEventBus`.

`Granit.IoT.Aws.Jobs` consumes that event and turns each delta key into
an AWS IoT Job dispatched against the originating Thing. The
correlationId is `SHA-256(deviceId, shadowVersion)` so re-delivery
dispatches into the same Job.

## Fleet Provisioning (JITP)

`Granit.IoT.Aws.FleetProvisioning` handles the inverted flow where AWS
IoT creates the Thing and certificate **before** notifying Granit:

1. Device authenticates with its claim certificate.
2. AWS IoT triggers a customer-owned Lambda.
3. Lambda calls `POST /api/iot/fleet-provisioning/verify` — Granit
   validates against the deny-list (decommissioned serials are denied).
4. AWS creates the Thing and the operational cert.
5. Lambda calls `POST /api/iot/fleet-provisioning/registered` — Granit
   atomically materialises the `Device` aggregate (in `Active` status)
   and the `AwsThingBinding` (`ProvisionedViaJitp = true`).

The standard saga handler short-circuits on `ProvisionedViaJitp == true`
to avoid attempting to recreate a Thing that AWS already created.
`ClaimCertificateRotationCheckService` runs daily and surfaces
expiring claim certificates as `ClaimCertificateExpiringEvent` so
operators rotate them before fleet enrolments break.

## Wiring

```csharp
builder.Services.AddGranitIoTAwsCredentials();           // IAwsIoTCredentialProvider pipeline
builder.Services.AddGranitIoTAwsEntityFrameworkCore(o => // AwsBindingDbContext
    o.UseNpgsql(connectionString));
builder.Services.AddGranitIoTAwsProvisioning();          // saga + Secrets Manager loader
builder.Services.AddGranitIoTAwsShadow();                // shadow bridge
builder.Services.AddGranitIoTAwsJobs();                  // command dispatcher
builder.Services.AddGranitIoTAwsFleetProvisioning();     // JITP service + rotation check

// Endpoints
app.MapGranitIoTAwsFleetProvisioningEndpoints();
```

Or, for the lazy:

```csharp
builder.Services.AddGranitBundleIoTAws(o => o.UseNpgsql(connectionString));
```

## Configuration

Each package has its own configuration section under `IoT:Aws:*`. The
defaults are production-sane; override per environment.

| Section | Knobs |
| ------- | ----- |
| `IoT:Aws:Credentials` | `FleetCredentialSecretArn`, `RotationCheckIntervalMinutes`, `InitialFetchTimeoutSeconds` |
| `IoT:Aws:Provisioning` | `DevicePolicyName` (required), `SecretNameTemplate`, `SecretKmsKeyId` |
| `IoT:Aws:Shadow` | `PollIntervalSeconds`, `PollBatchSize`, `AutoPushLifecycleStatus` |
| `IoT:Aws:Jobs` | `JobIdPrefix`, `JobTrackingTtlHours`, `StatusPollIntervalSeconds`, `StatusPollBatchSize` |
| `IoT:Aws:FleetProvisioning` | `ExpiryWarningWindowDays`, `RotationCheckIntervalHours`, `RotationCheckBatchSize` |

## Observability

Every package ships a dedicated meter (`Granit.IoT.Aws.*`) tagged with
`tenant_id` and, where it makes sense, the matching `operation`. Dashboards
can roll up across the family or split per package. The architecture
tests enforce that every `Internal` namespace stays internal-only.

## Operational caveats

- **`InMemoryJobTrackingStore`** is per-host. Multi-host deployments must
  override `IJobTrackingStore` with a Redis-backed implementation.
- **Polling vs IoT Rule**. The Shadow and Jobs polling services are
  fine up to a few thousand devices per host. Larger fleets should
  switch to AWS IoT Rule → SNS / SQS — the bridge is wired through
  `ILocalEventBus.PublishAsync`, so dropping in an event-driven
  consumer is mechanical.
- **Cert leak window**. A crash strictly between
  `CreateKeysAndCertificate` and the matching DB commit leaks one AWS
  certificate. Surfaced via the
  `granit.iot.aws.provisioning.failed` counter; a follow-up story will
  add a sweeper that deletes certificates with no attached principal.

## Compliance

- **GDPR**: AWS resources tied to a tenant are deleted by the
  `DecommissionAsync` path on `DeviceDecommissionedEvent` — Thing,
  certificate, secret and binding all torn down idempotently.
- **ISO 27001**: every saga transition is logged via `LoggerMessage`
  source-gen with structured fields, every counter is tagged by
  `tenant_id`, and the `AwsThingBinding` aggregate is
  `FullAuditedAggregateRoot` so CreatedBy / ModifiedBy / DeletedBy
  travel with every row.
