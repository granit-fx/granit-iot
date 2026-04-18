# Security Policy

## Supported Versions

| Version | Supported |
| ------- | --------- |
| latest  | Yes       |

Only the latest released version receives security updates.

## Reporting a Vulnerability

**DO NOT open a public GitHub issue for security vulnerabilities.**

### Preferred: GitHub Private Security Advisory

Use [GitHub Private Security Advisories](https://github.com/granit-fx/granit-iot/security/advisories/new)
to report vulnerabilities confidentially. This enables coordinated disclosure
and automatic CVE assignment through MITRE.

### Alternative: Email

Send a detailed report to **<security@granit-fx.dev>**.

### What to include

- Steps to reproduce (proof of concept if possible)
- Potential impact and attack scenario
- Any suggested mitigations

## Response SLA

| Severity     | Acknowledgment | Patch target | Public disclosure   |
| ------------ | -------------- | ------------ | ------------------- |
| Critical     | 24 hours       | 7 days       | 14 days after patch |
| High         | 48 hours       | 30 days      | 30 days after patch |
| Medium / Low | 48 hours       | 90 days      | 90 days after patch |

## Out of scope

- Issues requiring physical access to infrastructure
- Social engineering attacks
- Denial-of-service through resource exhaustion

## Security Design

This module family is built on the [Granit framework](https://github.com/granit-fx/granit-dotnet),
which provides security primitives (OIDC/OpenIddict, RBAC, encryption, audit trail, GDPR).
For framework-level security issues, refer to the
[Granit security policy](https://github.com/granit-fx/granit-dotnet/security/policy).

## Trust boundaries the host must enforce

The packages in this repository assume the host wires up the following controls.
Missing any of these is a security defect at the integration layer — flag it in
code review before shipping.

### MQTT bridge — broker is the authoritative identity source

`Granit.IoT.Mqtt.Mqttnet` is a **client** of the broker. It trusts two things:

1. The broker has validated the device's mTLS client certificate at CONNECT.
2. The broker's per-device ACL guarantees that a client can only publish to
   topics whose `devices/{serial}/...` segment matches its own identity.

The bridge extracts the device serial from the topic and resolves the tenant
via `IDeviceLookup`. It does **not** perform an independent client-cert-to-topic
cross-check. **If the broker ACL is misconfigured, cross-tenant telemetry
spoofing becomes possible** — a compromised device can publish to
`devices/OTHER-SERIAL/telemetry` and the message lands under the victim
tenant. Operators must:

- Enforce broker-side ACL that binds each mTLS certificate subject/SAN to a
  single `devices/{serial}/#` topic tree.
- Audit broker ACL policy changes the same way they audit IAM changes.
- Monitor the `granit.iot.mqtt.*` counters for sustained unknown-device
  ingestion, which is the first signal of a broker-ACL failure.

Hosts with strict cross-tenant isolation requirements should add a custom
`IInboundMessageParser` wrapper that cross-checks the bridge's resolved serial
against a broker-supplied client-identity header (if available) before
delegating to `MqttMessageParser`.

### Fleet provisioning — tenant is server-side authoritative

`MapGranitIoTAwsFleetProvisioningEndpoints(authorizationPolicyName, …)` requires
an explicit authorization policy — the extension throws at startup without one.
The `FleetProvisioningService` then:

- derives the effective tenant from `ICurrentTenant.Id` (populated from the
  authenticated principal),
- rejects any body-supplied `TenantId` that doesn't match the principal,
- refuses to re-register a serial that is already bound to a different tenant.

The body field remains in the contract so a customer Lambda can keep a single
code path, but it is informational only.

### Ingestion webhook — provider signature is the authenticator

`POST /iot/ingest/{source}` is deliberately unauthenticated at the ASP.NET
layer; the provider-specific validator (SigV4, SNS RSA, HMAC-SHA256) is the
authentication. The endpoint rejects payloads larger than 256 KB before buffering,
applies the `iot-ingest` rate-limit policy, and strips client-supplied
`granit-request-*` headers before forwarding to the validator. Operators must:

- Configure `IoT:Ingestion:Scaleway:SharedSecret` via `Granit.Vault` only —
  the options validator fails startup if the value lives in appsettings in
  non-`Development` environments.
- Pin `IoT:Ingestion:Aws:Sns:TopicArnPrefix` to the tenant's IoT topic namespace
  so cross-account replay noise is rejected before the RSA verification.

### GDPR integration — host wires the locator

The core `Granit.IoT` module exposes `IIoTDataSubjectLocator` as the integration
point between `Granit.Privacy`'s export/deletion sagas and IoT-owned data.
Because devices are bound to tenants rather than users, the user↔device
mapping is host-specific (HR system, wearable registry, asset catalog).

The default registration is `NullIoTDataSubjectLocator`, which reports zero
devices — this keeps the module compliant-by-default (no personal data is
claimed for a user the IoT module cannot resolve) but means operators running
Granit.Privacy **must** replace the registration if their IoT data is
user-attributable. A follow-up companion package (`Granit.IoT.Privacy`) will
register the module with the `IDataProviderRegistry` and wire the Wolverine
handlers for `PersonalDataRequestedEto` / `PersonalDataDeletionRequestedEto`
against the locator.

## Dependency pinning & supply chain

`Directory.Packages.props` uses floating patterns (`4.0.*`, `5.31.*`, `10.*`,
`0.1.0-dev.*`). This is a deliberate trade-off: it tracks upstream security
fixes without daily churn, but it is **incompatible with reproducible builds
in production environments**. Hosts that require reproducibility must:

- Commit `packages.lock.json` per project and set `RestorePackagesWithLockFile=true`.
- Pin cryptographic/network dependencies (AWSSDK.\*, MQTTnet, JwtBearer,
  StackExchange.Redis) to exact versions in their own `Directory.Packages.props`
  override.
- Enable `signatureValidationMode="require"` in `nuget.config` and mirror the
  feed through a trusted internal registry.

## Recognition

Reporters of valid vulnerabilities will be credited in the release notes
(unless they prefer to remain anonymous).
