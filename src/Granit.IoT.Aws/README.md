# Granit.IoT.Aws

Bridge package mapping Granit IoT devices to AWS IoT Core resources (Things,
certificates, Secrets Manager entries). Sits in **Ring 3** alongside
`Granit.IoT.Notifications` and `Granit.IoT.Timeline`: it reacts to the
lifecycle events raised by the core `Device` aggregate without ever mutating
it.

## Why a companion, not a second aggregate

A `Device` already owns the cloud-agnostic concerns: `SerialNumber`, `Status`,
heartbeat, credentials, workflow state, timeline. Duplicating that aggregate
to add AWS-specific fields would create two sources of truth and force a
permanent synchronisation problem.

`AwsThingBinding` is therefore a 1:1 companion keyed by `DeviceId`. It carries
only what AWS needs:

- `ThingName` (value object, format `t{tenantId:N}-{serialNumber}` for IAM
  policy isolation)
- `ThingArn`
- `CertificateArn` (X.509 cert ARN in AWS IoT)
- `CertificateSecretArn` (AWS Secrets Manager ARN that holds the private key)
- `ProvisioningStatus` (idempotency checkpoint — see below)
- `LastShadowReportedAt`, `ClaimCertificateExpiresAt` (operational metadata)

## Idempotent provisioning saga

AWS IoT APIs (`CreateThing`, `CreateKeysAndCertificate`) are not idempotent,
and Wolverine guarantees at-least-once delivery. The provisioning workflow is
therefore modelled as a saga whose state lives on `AwsThingBinding`:

```text
Pending → ThingCreated → CertIssued → SecretStored → Active
                                                        ↓
                                                Decommissioned
                                       (or Failed for non-recoverable errors)
```

Each handler step (`Ensure*Async`, shipped in PR #4) reads the current status,
calls the matching AWS API only if the checkpoint has not been crossed yet,
and persists the new status + ARN inside the same transaction as the Wolverine
inbox acknowledgement. A replay therefore resumes exactly where it stopped —
never creating duplicate AWS resources.

## What this package contains today (PR #1, story #44)

- Companion aggregate `AwsThingBinding` (`FullAuditedAggregateRoot`,
  `IMultiTenant`)
- Value object `ThingName` with the imposed `t{tenantId:N}-{serialNumber}` format
- `AwsThingProvisioningStatus` enum with the saga checkpoints
- CQRS abstractions `IAwsThingBindingReader` / `IAwsThingBindingWriter`
- Domain events `AwsThingProvisionedEvent`, `AwsThingDecommissionedEvent`
- Module wiring `GranitIoTAwsModule`

The persistence (`AwsBindingDbContext`), the bridge handlers, the actual
provisioning service and the credential provider land in subsequent PRs
(stories #45 → #51).
