# Granit.IoT.Aws.FleetProvisioning

AWS IoT Fleet Provisioning (JITP) endpoints for the Granit IoT bridge.
When a device authenticates with its claim certificate, AWS IoT triggers
a customer-owned Lambda that calls the two endpoints in this package to
validate the device against Granit's deny-list and to materialise the
matching `Device` aggregate plus its `AwsThingBinding`.

```text
[device]──claim cert──▶[AWS IoT]──┬──▶ [customer Lambda]──POST /verify──▶ [Granit]
                                  │                          ◀── allow/deny ──
                                  ▼
                         [AWS creates Thing + cert]
                                  │
                                  └──▶ [customer Lambda]──POST /registered──▶ [Granit]
                                                                ◀── deviceId ──
```

## Why a separate package, not a saga step

The standard saga in `Granit.IoT.Aws.Provisioning` reacts to a Granit-side
`DeviceProvisionedEvent` and creates the AWS resources from scratch. JITP
inverts that: AWS creates the resources first, then notifies us. Running
the saga's `EnsureThing` after JITP would attempt to create a duplicate
Thing (and silently waste a certificate slot via
`CreateKeysAndCertificate`). The JITP path bypasses the saga by
materialising the binding straight in `Active` via
`AwsThingBinding.CreateForJitp(...)`, which sets the `ProvisionedViaJitp`
flag the saga handler short-circuits on (see PR #4).

## Endpoints

| Method + path | Body | Returns |
| ------------- | ---- | ------- |
| `POST /api/iot/fleet-provisioning/verify` | `FleetProvisioningVerifyRequest` | `FleetProvisioningVerifyResponse` (`allowProvisioning`, optional `reason`) |
| `POST /api/iot/fleet-provisioning/registered` | `FleetProvisioningRegisterRequest` | `FleetProvisioningRegisterResponse` (`deviceId`, `alreadyProvisioned`) |

`/verify` returns `allowProvisioning=false` when the serial number
belongs to a `Decommissioned` device. Every other case allows the JITP
flow to proceed.

`/registered` is idempotent on the serial number:

- If `Device + binding` already exist → returns the existing `deviceId`
  with `alreadyProvisioned=true`, no writes.
- If `Device` exists but binding is missing → creates only the binding.
- Otherwise → creates the Device (already in `Active` status) and the
  binding atomically.

## Wiring

```csharp
builder.Services.AddGranitIoTAwsFleetProvisioning();
// …
app.MapGranitIoTAwsFleetProvisioningEndpoints();
```

The customer's AWS Lambda holds the contract between AWS IoT and Granit
— it picks the JSON shape AWS IoT sends, translates it into the request
record above, and forwards Granit's verdict back to AWS.

## Claim-certificate rotation

`ClaimCertificateRotationCheckService` is a daily background sweep that
walks active bindings and raises
`ClaimCertificateExpiringEvent` (on `ILocalEventBus`) for every binding
whose `ClaimCertificateExpiresAt` falls inside `ExpiryWarningWindowDays`
(default 30). Hosts that have wired `Granit.IoT.Notifications` will
automatically deliver these events to the operator on-call channels.

`ClaimCertificateExpiresAt` is populated by callers (typically the same
Lambda that calls `/registered`) via the optional
`ClaimCertificateExpiresAt` request field. The sweep is in-memory after
the bindings are loaded, so the warning window changes are picked up on
the next tick without restart.

## Configuration

```jsonc
{
  "IoT": {
    "Aws": {
      "FleetProvisioning": {
        "ExpiryWarningWindowDays": 30,
        "RotationCheckIntervalHours": 24,
        "RotationCheckBatchSize": 1000
      }
    }
  }
}
```

## Metrics

| Counter | When it ticks |
| ------- | ------------- |
| `granit.iot.aws.jitp.verify_allowed` | Pre-hook returned `allowProvisioning=true`. |
| `granit.iot.aws.jitp.verify_denied` | Pre-hook returned `allowProvisioning=false`. |
| `granit.iot.aws.jitp.register_completed` | New Device + binding created. |
| `granit.iot.aws.jitp.register_idempotent` | Re-registration short-circuited (existing Device + binding). |
| `granit.iot.aws.jitp.claim_certificate_expiring` | Rotation sweep surfaced an expiring claim certificate. |

## What does not land

- **Claim certificate auto-rotation** — only detection. Rotating the
  claim cert itself is a deliberately manual operation; the warning
  events buy lead time.
- **Authentication of the JITP endpoints** — host responsibility. The
  recommended setup is mTLS or a shared HMAC between the customer's
  Lambda and the Granit endpoint, both behind a private VPC.
