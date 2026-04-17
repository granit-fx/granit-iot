# Granit.IoT.Aws.Provisioning

AWS-SDK-backed companion of [Granit.IoT.Aws](../Granit.IoT.Aws/README.md).
Hosts the saga that creates and tears down the matching AWS resources for
every `AwsThingBinding`, plus the Secrets Manager-backed
`IAwsIoTCredentialLoader` that completes the credential pipeline.

This is the **first** package in the `Granit.IoT.Aws` family to depend on
`AWSSDK.Core`, `AWSSDK.IoT` and `AWSSDK.SecretsManager`. Sister packages
(`Shadow`, `Jobs`, `FleetProvisioning`) take their AWS SDK references
individually so a host that does not need a particular capability never
ships its assembly closure.

## Saga walkthrough

The Wolverine handler `AwsThingBridgeHandler` reacts to
`DeviceProvisionedEvent` and walks the binding through:

```text
Pending → ThingCreated → CertIssued → SecretStored → Active
```

Each `Ensure*Async` method on `IThingProvisioningService`:

1. Short-circuits if the binding has already crossed the matching checkpoint
   (lock-free read of `binding.ProvisioningStatus`).
2. Defensively calls the AWS `Describe*` API before issuing a `Create*` —
   covers the case where a previous attempt succeeded on AWS but failed to
   commit the binding update.
3. Mutates the in-memory binding (`RecordThingCreated`,
   `RecordCertificateIssued`, ...) and returns. The caller persists the new
   state immediately afterwards through `IAwsThingBindingWriter.UpdateAsync`.

`EnsureCertificateAndSecretAsync` deliberately fuses two saga steps:
`CreateKeysAndCertificate` returns a private key that AWS will never give
back. We persist that key to Secrets Manager (with a `ClientRequestToken =
binding.Id` so the Secrets Manager call is natively idempotent) inside the
same logical operation as the cert creation. A crash between the cert call
and the secret call leaves a dangling certificate in AWS — surfaced by the
binding moving to `Failed` and visible in the
`granit.iot.aws.provisioning.failed` counter for an operator-driven sweep.

## Idempotence guarantees

- Wolverine delivers each event at-least-once.
- `Describe*` precedes every `Create*`, so a re-delivered handler does not
  duplicate AWS resources.
- `AttachPolicy` and `AttachThingPrincipal` are no-ops when the attachment
  exists.
- Secrets Manager `CreateSecret` uses `ClientRequestToken = binding.Id` —
  AWS returns the existing secret on retry instead of throwing.
- `MarkAsFailed` is reserved for the (rare) split-cert-secret case;
  everything else is left for a Wolverine retry.

## JITP bypass

Bindings created through the Fleet Provisioning flow (`CreateForJitp`) are
born in `Active` with `ProvisionedViaJitp = true`. The bridge handler
detects both flags and short-circuits the entire saga to avoid creating a
duplicate Thing for a device that AWS already provisioned through JITP.

## Decommission path

`DeviceDecommissionedEvent` triggers `DecommissionAsync`:

1. `DetachThingPrincipal`
2. `DetachPolicy`
3. `UpdateCertificate` → `INACTIVE`
4. `DeleteCertificate` (force=true)
5. `DeleteSecret` (force-delete-without-recovery)
6. `DeleteThing`

Each step swallows `ResourceNotFoundException` so a partial cleanup left
over from a previous attempt completes cleanly.

## Configuration

```jsonc
{
  "IoT": {
    "Aws": {
      "Provisioning": {
        "DevicePolicyName": "GranitIoTDeviceDefaultPolicy",
        "SecretNameTemplate": "iot/devices/{thingName}/private-key",
        "SecretKmsKeyId": null
      },
      "Credentials": {
        "FleetCredentialSecretArn": "arn:aws:secretsmanager:eu-west-1:123:secret:fleet-AbC"
      }
    }
  }
}
```

The credential `FleetCredentialSecretArn` switches the rotating provider
on; this package's `AwsSecretsManagerCredentialLoader` then becomes the
loader the rotating provider polls.

## Metrics

The `Granit.IoT.Aws.Provisioning` meter publishes:

| Counter | When it ticks |
| ------- | ------------- |
| `granit.iot.aws.thing.created` | A new Thing was created (replays don't count). |
| `granit.iot.aws.certificate.issued` | A new X.509 cert + Secrets Manager entry were created. |
| `granit.iot.aws.binding.activated` | Binding reached `Active`. |
| `granit.iot.aws.binding.decommissioned` | Binding reached `Decommissioned`. |
| `granit.iot.aws.provisioning.failed` | Binding moved to `Failed` — operator must reconcile. |

Every counter is tagged with `tenant_id` (coalesced to `"global"` for
unscoped bindings).
