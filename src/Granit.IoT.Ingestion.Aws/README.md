# Granit.IoT.Ingestion.Aws

AWS IoT Core ingestion provider for Granit.IoT.

Without this package, an application using Granit.IoT can ingest device
telemetry from Scaleway IoT Hub and from any MQTT broker — but not from AWS.
This package implements the three architecturally distinct HTTP paths AWS IoT
Core publishes to (SNS, direct HTTP, API Gateway) and plugs into the existing
provider-agnostic `POST /iot/ingest/{source}` endpoint. No new endpoints, no
new pipeline — just the validators, parsers, and the cert cache that AWS
specifically requires.

All three AWS IoT Core paths are now wired end-to-end through the
provider-agnostic `POST /iot/ingest/{source}` endpoint. Deliveries from AWS
hit `/iot/ingest/awsiotsns`, `/iot/ingest/awsiotdirect`, or
`/iot/ingest/awsiotapigw` and flow through the same pipeline (signature
validation → parse → dedup → outbox dispatch) as Scaleway, just with
provider-specific validators and parsers.

## What this slice ships

- `GranitIoTIngestionAwsModule` — depends on `GranitIoTIngestionModule` +
  `GranitCachingModule`; registers options, validator, cert cache, metrics
- `AwsIoTIngestionOptions` — three sub-sections (`Sns`, `Direct`, `ApiGateway`),
  each independently `Enabled`
- `AwsIoTIngestionOptionsValidator` (`IValidateOptions`) — fails startup when
  no path is enabled, when an enabled path lacks a region, or when a
  `Direct.ApiKey` is set in non-Development environments
- `ISnsSigningCertificateCache` + `DefaultSnsSigningCertificateCache` — fetches
  the AWS RSA signing cert once per `CertCacheHours`, backed by `IFusionCache`
  (from `Granit.Caching`)
- `SnsPayloadSignatureValidator` (`IPayloadSignatureValidator`,
  `SourceName = "awsiotsns"`) — RSA-SHA256 verification, replay dedup,
  topic-ARN allow-list, optional auto-confirmation of `SubscriptionConfirmation`
- `AwsIoTIngestionMetrics` — OpenTelemetry counters
  (`granit.iot.aws.ingestion.sns.*`, `granit.iot.aws.ingestion.sigv4.*`)
- `SnsSigningCertFetchException` — surfaced as `503 Service Unavailable` by
  the endpoint layer
- `ISigV4RequestValidator` + `DefaultSigV4RequestValidator` — reusable AWS
  Signature V4 verifier matching the canonical
  [AWS SigV4 test suite](https://docs.aws.amazon.com/general/latest/gr/sigv4_signing.html)
  (5-minute clock skew, scope-date check, `FixedTimeEquals` comparison)
- `ISigV4SigningKeyProvider` — host-supplied secret-key resolver, typically
  reading from `Granit.Vault`. Implementations must return `null` for
  unknown access keys so the validator can fail closed.
- `DirectPayloadSignatureValidator` (`SourceName = "awsiotdirect"`) —
  dual-mode: Bearer API key in Development, SigV4 everywhere else
- `ApiGatewayPayloadSignatureValidator` (`SourceName = "awsiotapigw"`) —
  SigV4 only
- `AwsIoTRulePayloadParser` — parses the IoT Rule JSON envelope emitted by
  a SELECT rule (`messageId`, `deviceId`, `timestamp`, `metrics`); registered
  twice, once for Direct and once for API Gateway
- `AwsIoTSnsPayloadParser` — strips the outer SNS envelope and delegates to
  the IoT Rule parser on the inner `Message` string

## IoT Rule SQL shape

Your AWS IoT Rule must SELECT fields that match `AwsIoTRuleEnvelope`:

```sql
SELECT
    newuuid() AS messageId,
    clientId() AS deviceId,
    timestamp() AS timestamp,    -- Unix ms, or use parse_time('yyyy-MM-dd''T''HH:mm:ss.SSSZ', timestamp()) AS timestamp for ISO-8601
    payload AS metrics           -- object: {"temperature": 22.5, ...}
FROM 'granit/telemetry/+'
```

The parser accepts `timestamp` as either Unix milliseconds (what
`timestamp()` returns) or an ISO-8601 string.

## Two layers of cert security

```mermaid
flowchart LR
  REQ["POST /iot/ingest/awsiotsns"]
  ENV["Parse SNS envelope"]
  REGEX{"SigningCertURL matches<br/>AWS CDN regex?"}
  REJECT["Reject (Invalid)"]
  CACHE{"Cert in cache?"}
  FETCH["GET cert from CDN"]
  RSA["RSA.VerifyData(canonical, sig)"]
  OK["Accept"]

  REQ --> ENV --> REGEX
  REGEX -->|no| REJECT
  REGEX -->|yes| CACHE
  CACHE -->|hit| RSA
  CACHE -->|miss| FETCH --> RSA
  RSA -->|verified| OK
  RSA -->|fail| REJECT
```

1. **CDN allow-list** — the cert URL must match
   `https://sns.{region}.amazonaws.com/SimpleNotificationService-*.pem`
   (a `[GeneratedRegex]` checked **before** any HTTP call). An attacker who
   controls the SNS message body cannot redirect us to fetch their own cert.
2. **RSA-SHA256** — `RSA.VerifyData(canonical, sig, SHA256, Pkcs1)` against
   the cached public key. Failure invalidates the cached entry so the next
   request re-fetches (covers AWS key rotation).

## Setup

```csharp
builder.Services
    .AddGranit(builder.Configuration)
    .AddModule<GranitIoTModule>()
    .AddModule<GranitIoTIngestionModule>()
    .AddModule<GranitIoTIngestionAwsModule>();

app.MapGranitIoTIngestionEndpoints();
// AWS SNS deliveries hit POST /iot/ingest/awsiotsns
```

## Configuration

```jsonc
{
  "IoT": {
    "Ingestion": {
      "Aws": {
        "Sns": {
          "Enabled": true,
          "Region": "eu-west-1",
          "TopicArnPrefix": "arn:aws:sns:eu-west-1:123456789012:iot-",
          "AutoConfirmSubscription": false,
          "CertCacheHours": 24,
          "DeduplicationWindowMinutes": 5
        },
        "Direct": { "Enabled": false, "Region": "" },
        "ApiGateway": { "Enabled": false, "Region": "" }
      }
    }
  }
}
```

| Setting | Default | Purpose |
| --- | --- | --- |
| `Sns:Enabled` | `false` | Master switch for the SNS path |
| `Sns:Region` | _(required when enabled)_ | Used by future paths and for cert URL hostname checks |
| `Sns:TopicArnPrefix` | `null` | Optional fast-fail filter — reject foreign topics before RSA work |
| `Sns:AutoConfirmSubscription` | `false` | Fire-and-forget GET to `SubscribeURL` on `SubscriptionConfirmation` |
| `Sns:CertCacheHours` | `24` | Per-cert TTL in `IFusionCache` |
| `Sns:DeduplicationWindowMinutes` | `5` | Replay window per `MessageId` (matches SNS at-least-once SLA) |
| `SigningKeyCacheHours` | `24` | Per-scope SigV4 signing-key TTL — keys change once per calendar day so 24h is the natural ceiling |

> [!IMPORTANT]
> `Direct:ApiKey` MUST NOT be set in `appsettings.{Production}.json`. The
> options validator fails startup if it finds a value outside the
> `Development` environment. Load it from `Granit.Vault` at runtime and bind
> via `IOptionsMonitor<AwsIoTIngestionOptions>` — secret rotations apply
> without restart.

## SigV4 — how it fits

```mermaid
flowchart LR
  REQ["Direct / API Gateway request"]
  HDR["Authorization: AWS4-HMAC-SHA256<br/>x-amz-date"]
  SKEW{"Within 5-min<br/>clock skew?"}
  SCOPE{"scope.date == x-amz-date.day?"}
  PROV["ISigV4SigningKeyProvider<br/>(Granit.Vault)"]
  DERIVE["SigV4SigningKey.Derive()<br/>(HMAC chain, cached per scope)"]
  COMPUTE["Build canonical request<br/>+ string-to-sign<br/>+ HMAC-SHA256"]
  CMP{"FixedTimeEquals<br/>(computed, received)?"}
  OK["Accept"]
  REJECT["Reject"]

  REQ --> HDR --> SKEW
  SKEW -->|no| REJECT
  SKEW -->|yes| SCOPE
  SCOPE -->|no| REJECT
  SCOPE -->|yes| PROV --> DERIVE --> COMPUTE --> CMP
  CMP -->|match| OK
  CMP -->|mismatch| REJECT
```

The validator is verified against the AWS public
[`get-vanilla` test vector](https://docs.aws.amazon.com/general/latest/gr/sigv4-create-canonical-request.html) —
it reproduces the canonical request, string-to-sign, and signature
(`5fa00fa31553b73ebf1942676e86291e8372ff2a2260956d9b8aae1d763fbf31`) byte for
byte. Any drift from the AWS spec breaks that test before merge.

### Server-controlled request metadata

SigV4 canonical requests include the HTTP method, path, and query string —
none of which are reachable from `(body, headers)` alone. The ingestion
endpoint injects three synthetic headers
(`granit-request-method` / `-path` / `-query`) **after stripping any
client-provided values with the same prefix**. The validators read those
synthetic headers; malicious callers cannot lie about what they signed
because their `granit-request-*` headers are overwritten before the
validator sees them.

Trust boundary: `Granit.IoT.Ingestion.Endpoints` — see
`IngestionEndpoints.InjectServerControlledRequestHeaders`.

## Anti-patterns to avoid

> [!WARNING]
> **Don't widen the CDN regex** to accept arbitrary `.amazonaws.com` paths.
> The pattern is a security boundary; an attacker who controls the SNS
> message body could otherwise host a malicious cert at any S3 bucket and
> bypass signature verification.

> [!WARNING]
> **Don't disable replay dedup to "improve throughput".** SNS guarantees
> at-least-once delivery; the 5-minute window protects against double
> ingestion, not against bad actors.

> [!CAUTION]
> **`SubscriptionConfirmation` auto-confirmation is opt-in for a reason.**
> Auto-confirming a subscription accepts the contract that the subscribed
> topic publishes to your endpoint. Leave `AutoConfirmSubscription = false`
> in production unless you control the SNS topic policy too.

## See also

- [`Granit.IoT.Ingestion`](../Granit.IoT.Ingestion/README.md) — the pipeline this provider plugs into
- [`Granit.IoT.Ingestion.Scaleway`](../Granit.IoT.Ingestion.Scaleway/README.md) — sister provider, same shape
- [Telemetry ingestion deep dive](../../docs/telemetry-ingestion.md) — end-to-end flow
