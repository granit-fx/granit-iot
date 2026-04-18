# Threat Models — Granit.IoT

Pre-built STRIDE threat models for the most critical Granit.IoT components.
Used by the `/security` skill as starting points — adapt based on actual code
analysis.

---

## Trust Boundaries

```text
ZONE 1 — Physical / Hostile (Field)
├── Physical IoT devices (sensors, gateways, actuators)
├── Device firmware (potentially cloneable, extractable)
└── Local networks (often untrusted, sometimes compromised)

ZONE 2 — Cloud Provider Edge
├── AWS IoT Core (MQTT broker + Rules Engine)
├── Scaleway IoT Hub
├── AWS SNS (delivery of device events to webhooks)
└── Cloud provider identity (IAM, Scaleway IAM)

ZONE 3 — Granit.IoT Ingestion Edge
├── POST /iot/ingest/{source} endpoint
├── SigV4 validator / SNS cert cache (AWS)
├── Scaleway webhook authenticator
├── MQTT broker (Granit.IoT.Mqtt.Mqttnet — if self-hosted)
├── Rate limiter
└── Deduplication (Redis)

ZONE 4 — Internal (Application)
├── Ingestion pipeline (Granit.IoT.Ingestion.*)
├── Wolverine outbox + handlers (Granit.IoT.Wolverine)
├── Device aggregate, credentials (Granit.IoT domain)
├── Fleet provisioning (Granit.IoT.Aws.FleetProvisioning)
├── Background jobs (purge, heartbeat)
└── MCP server (Granit.IoT.Mcp)

ZONE 5 — Trusted (Data)
├── PostgreSQL (Device, Telemetry JSONB, Timeline)
├── TimescaleDB hypertables (optional)
├── Redis (cache, dedup, rate limiting)
├── Vault (provider credentials, signing keys)
└── Blob storage (claim check payloads, archives)

ZONE 6 — AI / External Consumers
├── MCP client (AI agent)
├── Notification publisher bridge (Granit.IoT.Notifications → INotificationPublisher)
└── Timeline writer (Granit.IoT.Timeline → ITimelineWriter)
```

---

## TM-01: Telemetry Ingestion (AWS SNS → Webhook)

**Data Flow:** Device → AWS IoT Core (MQTT) → IoT Rule → SNS topic → HTTPS
POST to `/iot/ingest/aws` → SigV4 validator → SNS cert validator → Provider
parser → Device resolution → Wolverine outbox → `TelemetryIngestedHandler`
→ PostgreSQL (Telemetry)

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Spoofing** | Attacker POSTs crafted payload to `/iot/ingest/aws` | `ISigV4RequestValidator` + SNS cert chain validation BEFORE parsing | Check middleware ordering, validator implementation |
| **Spoofing** | Attacker replays valid SNS message | `X-Amz-Date` 5-minute skew window + idempotency key | Check skew tolerance and dedup scope |
| **Spoofing** | Attacker forges SNS signing cert URL | Cert URL pattern pinned to `*.amazonaws.com` | Check `SnsSigningCertificateCache` pattern |
| **Spoofing** | Attacker sends payload from their own AWS account | AWS account ID validated against tenant's bound account | Check payload validation step |
| **Tampering** | Payload modified in transit | HTTPS + SigV4 signature validation | Check TLS enforcement + SigV4 |
| **Repudiation** | Cannot correlate ingestion with device | Trace context propagated, ingestion audit log | Check `TraceContextBehavior` |
| **Info Disclosure** | Response leaks whether device is known | Always return 202 Accepted regardless of device status | Check `IngestionEndpoints` response code |
| **Info Disclosure** | Error messages reveal internal state | RFC 7807 problem details, sanitized errors | Check error handler |
| **DoS** | Flood ingestion endpoint | Per-tenant + per-device rate limits BEFORE signature validation | Check rate limiter ordering |
| **DoS** | Huge payload (100 MB) exhausts memory | Kestrel `MaxRequestBodySize` + Wolverine envelope cap | Check Kestrel config |
| **DoS** | Dedup Redis unavailable → fail-open creates flood | Fail-closed policy (reject new messages if dedup down) | Check `CounterStoreFailureBehavior` |
| **EoP** | Device payload claims a tenantId — server accepts it | Tenant derived from device binding, payload `tenantId` ignored | Check `TelemetryIngestedHandler` |
| **EoP** | Malformed payload triggers type instantiation | `System.Text.Json` with closed polymorphic types | Check serializer config |

### Attack Tree: Cross-Tenant Telemetry Injection

```text
Goal: Inject telemetry appearing to come from Tenant B while authenticated as Tenant A
├── [OR] Spoof AWS SNS source
│   ├── Use Tenant A's own AWS account to send to Granit webhook
│   │   └── Mitigated by: account ID validation against tenant binding
│   ├── Forge SigV4 signature
│   │   └── Mitigated by: signature validation with bound AWS access key ID
│   └── Replay Tenant B's captured legitimate message
│       └── Mitigated by: idempotency (dedup on message ID + skew window)
├── [OR] Claim Tenant B ownership in payload body
│   ├── Include `tenantId` field in telemetry
│   │   └── Mitigated by: server-side tenant from device binding, payload field ignored
│   └── Include another device's ID
│       └── Mitigated by: device must belong to authenticated tenant
└── [OR] Race condition at device provisioning
    ├── Provision device, switch tenant binding, send telemetry during race
    │   └── Mitigated by: binding is immutable after provisioning
    └── Register same device ID in two tenants
        └── Mitigated by: device ID uniqueness globally OR per-tenant with disambiguation
```

---

## TM-02: Fleet Provisioning (AWS)

**Data Flow:** Device → MQTT CONNECT with claim cert → AWS IoT Core →
Provisioning template → `CreateCertificateFromCsr` → AWS returns operational
cert + Thing → Granit webhook notification → Device-tenant binding persisted

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Spoofing** | Attacker presents stolen claim cert | Claim certs short-lived, tenant-scoped, revocable | Check cert lifecycle |
| **Spoofing** | Attacker provisions device under Tenant B using Tenant A's template | Provisioning template scoped per tenant (no shared template) | Check `Granit.IoT.Aws.FleetProvisioning` |
| **Tampering** | Modify CSR to inject weak key | CSR validation rejects <2048 RSA, non-P256/P384 ECDSA | Check CSR validator |
| **Tampering** | Modify provisioning template to grant broad policies | Template changes require elevated permissions + audit | Check template management |
| **Repudiation** | Device provisioned without audit | Provisioning audit trail (who/what/when/device/tenant) | Check audit on provisioning |
| **Info Disclosure** | Registration token leaks in logs | Tokens treated as secrets, masked in logs | Check logging templates |
| **DoS** | Flood provisioning endpoint | Rate limit per claim cert / per tenant | Check rate limit coverage |
| **EoP** | Provisioning template grants `iot:*` | Least-privilege policy in template | Check template policy document |
| **EoP** | Race between cert creation and tenant binding — orphaned cert usable | Atomic binding: if binding fails, revoke cert | Check saga or transactional flow |

### Attack Tree: Unauthorized Device Enrollment

```text
Goal: Enroll a device under Tenant B without authorization
├── [OR] Obtain Tenant B's claim cert
│   ├── Steal from compromised device in the field
│   │   └── Mitigated by: claim certs short-lived + per-device
│   ├── Extract from another Tenant B device's storage
│   │   └── Mitigated by: HSM / secure element on device (out of backend scope)
│   └── Intercept during initial provisioning
│       └── Mitigated by: TLS + pre-shared out-of-band delivery
├── [OR] Bypass template tenant scoping
│   ├── Use Tenant B's template with Tenant A's claim cert
│   │   └── Check: template validates caller identity against tenant binding
│   └── Modify provisioning parameters to override tenant ID
│       └── Check: tenant derived from template + caller, not from parameters
└── [OR] Replay provisioning flow
    ├── Replay CSR submission for already-provisioned device
    │   └── Check: CSR uniqueness enforced (nonce / counter)
    └── Use captured registration token after legitimate enrollment
        └── Check: tokens single-use, time-bounded
```

---

## TM-03: Multi-Tenant Telemetry Query

**Data Flow:** Request (user or MCP agent) → Tenant resolution → Repository
query → EF query filter → PostgreSQL → Telemetry rows → MCP output sanitizer
→ Response

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Spoofing** | Attacker forges tenant context | Tenant from JWT claim validated against known tenants | Check tenant resolver chain |
| **Tampering** | Entity `TenantId` modified after creation | `private set` + interceptor-only | Check entity config |
| **Info Disclosure** | Query returns Tenant B rows | `ApplyGranitConventions` applied to Device + Telemetry | Check filter registration |
| **Info Disclosure** | `IgnoreQueryFilters()` in MCP tool | Audit all occurrences; none in MCP tools | Grep + review |
| **Info Disclosure** | `ExecuteDelete` in purge job bypasses filters | Explicit `.Where(e => e.TenantId == tenantId)` | Check `Granit.IoT.BackgroundJobs` |
| **Info Disclosure** | JSONB query on `Metrics` returns all tenants | Tenant filter applied before GIN-indexed JSONB query | Check query patterns |
| **Info Disclosure** | Cache key collision across tenants | Cache keys prefixed with tenant ID | Check cache key construction |
| **DoS** | Noisy tenant monopolizes DB | Tenant-partitioned rate limit + connection pooling | Check resource isolation |
| **EoP** | Tenant admin creates cross-tenant binding | Binding creation scoped by admin's tenant | Check binding endpoint authorization |

---

## TM-04: MCP Tool Execution on IoT Fleet

**Data Flow:** AI Agent → MCP transport → MCP server → Tenant-aware
visibility filter → IoT tool resolution → Authorization → Tool execution →
Output sanitizer → Response

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Spoofing** | AI agent impersonates another user | MCP transport authenticated (session, API key, mTLS) | Check MCP auth middleware |
| **Spoofing** | Tool executes under wrong tenant | `McpTenantScopeAttribute` + visibility filter | Check attribute coverage on IoT tools |
| **Tampering** | Prompt injection via device name | Device names validated on creation; escaped in tool output | Check device name validation + MCP response DTOs |
| **Tampering** | Tool response contains untrusted payload echoed to LLM | `IMcpOutputSanitizer` applied to all responses | Check sanitizer pipeline |
| **Repudiation** | AI denies performing fleet operation | All MCP tool calls audited (who, what, tenant, devices affected) | Check MCP audit |
| **Info Disclosure** | Tool returns cross-tenant telemetry | Tenant-scoped query filters enforced | Check tool implementations |
| **Info Disclosure** | Bulk export tool dumps entire fleet | Pagination + result caps | Check tool options |
| **Info Disclosure** | Error leaks connection string / internal path | Error sanitizer active on MCP error path | Check error handling |
| **DoS** | Recursive tool chain | Max depth bounded; per-user rate limit | Check execution pipeline |
| **EoP** | Tool chain escalates privileges | Each call re-validates permissions | Check authorization per call |
| **EoP** | Fleet provisioning tool exposed to low-privileged agent | Sensitive tools require elevated permissions | Check tool permission requirements |

### Attack Tree: Cross-Tenant Fleet Exfiltration via MCP

```text
Goal: Extract Tenant B's device roster while acting as Tenant A's agent
├── [OR] Bypass tenant visibility filter
│   ├── Tool not decorated with McpTenantScopeAttribute
│   │   └── Check: all data-accessing IoT tools have attribute
│   ├── Filter treats null tenant as "all tenants"
│   │   └── Check: `TenantAwareVisibilityFilter` null handling
│   └── Tool uses raw SQL bypassing EF filters
│       └── Check: no raw SQL in Granit.IoT.Mcp/Tools
├── [OR] Inject tenant ID in parameters
│   ├── Tool accepts `tenantId` as input
│   │   └── Check: tenant always from context, never from parameters
│   └── IDOR on device IDs across tenants
│       └── Check: device lookups always include tenant filter
└── [OR] Cache poisoning of tool response
    ├── Cached response from Tenant B served to Tenant A
    │   └── Check: response cache key includes tenant ID
    └── Stale cache after tenant context change
        └── Check: cache invalidation on context change
```

---

## TM-05: MQTT Broker (Granit.IoT.Mqtt.Mqttnet, if self-hosted)

**Data Flow:** Device → MQTT CONNECT (with client cert) → `ValidatingConnectionAsync`
→ MQTT session → PUBLISH / SUBSCRIBE → `InterceptingPublishAsync` /
`InterceptingSubscriptionAsync` → Message router → Granit.IoT ingestion

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Spoofing** | Attacker presents another device's cert | Client cert chain validated; subject CN matches device identity | Check `ValidatingConnectionAsync` |
| **Spoofing** | Username/password only (no cert) | mTLS mandatory for device auth | Check broker options |
| **Tampering** | Payload modified in transit | TLS 1.2+ | Check cipher suite policy |
| **Tampering** | Will message (LWT) on privileged topic | LWT topic subject to same ACL as regular publishes | Check LWT validation |
| **Info Disclosure** | Device subscribes to other tenants' topics | Topic ACL enforces `tenants/{tenantId}/devices/{deviceId}/#` | Check subscription filter |
| **Info Disclosure** | Retained message on cross-tenant topic | Retained messages ACL-checked on storage AND delivery | Check retained handling |
| **DoS** | QoS 2 message storm (unacked) | In-flight limit per session | Check MQTT server options |
| **DoS** | Oversized payload (MQTTnet default 256 MB) | `MaxPayloadSize` bounded per tenant | Check options configuration |
| **DoS** | Connection flood | Per-IP connection rate limit | Check broker layer |
| **EoP** | Device publishes to another device's command topic | Publish ACL per-topic | Check publish interceptor |

### Attack Tree: Cross-Device Topic Abuse

```text
Goal: Inject commands to another device from a compromised device
├── [OR] Publish to other device's command topic
│   ├── `tenants/T/devices/D1/cmd` published by D2
│   │   └── Check: InterceptingPublishAsync validates publisher identity == topic owner
│   └── Shared command topic accepted by broker
│       └── Check: no shared command topics, or ACL on shared topics
├── [OR] Subscribe to other device's telemetry
│   ├── Wildcard subscription `tenants/T/devices/+/telemetry`
│   │   └── Check: subscription interceptor rejects wildcards crossing device boundary
│   └── Topic alias abuse
│       └── Check: topic aliases resolved and re-validated against ACL
└── [OR] Retain malicious message for next subscriber
    ├── Publish retained message to a topic read by many devices
    │   └── Check: retained messages disabled or tightly ACL'd
```

---

## TM-06: SNS Signing Certificate Trust (AWS-specific)

**Data Flow:** AWS SNS sends signed message → Granit extracts
`SigningCertURL` → `SnsSigningCertificateCache` fetches + validates cert →
Verify signature → Process payload

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Spoofing** | Attacker controls `SigningCertURL` pointing to their own cert | URL pattern pinned to `*.amazonaws.com` | Check URL allowlist |
| **Spoofing** | DNS poisoning redirects `amazonaws.com` to attacker host | TLS validation on cert fetch (server cert pinned to AWS CA) | Check TLS config for cert fetch |
| **Tampering** | Cert cache poisoned with attacker's cert | Cache keyed by URL + thumbprint; validation on every use | Check cache implementation |
| **Tampering** | Clock skew abuse: accept expired cert | `NotAfter` enforced on cached cert | Check cache TTL vs cert validity |
| **Info Disclosure** | Cert fetch failure logs expose internal URLs | Logs sanitized | Check logging in `SnsSigningCertFetchException` |
| **DoS** | Cert fetch on every request exhausts network | Caching with TTL | Check cache hit ratio |
| **DoS** | Attacker-controlled URL triggers cert fetch (SSRF) | URL pattern allowlist rejects non-AWS URLs | Check pattern matching |
| **EoP** | Fetch failure = accept payload (fail-open) | Fail-closed: reject payload on cert fetch failure | Check error path |

---

## TM-07: Wolverine Ingestion Pipeline

**Data Flow:** Ingestion endpoint writes outbox record + telemetry to
PostgreSQL (single transaction) → Wolverine outbox agent polls → Dispatches
to `TelemetryIngestedHandler` → Handler validates device + tenant → Writes
telemetry row → Publishes downstream events

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Spoofing** | Fake message injected into outbox table | Outbox writes only via EF Core transaction (DB permissions restrict direct INSERT) | Check DB permissions |
| **Tampering** | Message content modified between outbox and consumer | Single-DB outbox (no external broker in the outbox step) OR envelope signed if using external broker | Check transport |
| **Tampering** | Claim check payload swapped in blob storage | Integrity hash validated on retrieval | Check claim check usage |
| **Repudiation** | Handler processed telemetry without trace | `TraceContextBehavior` applied | Check Wolverine config |
| **Info Disclosure** | DLQ contains telemetry with PII, accessed by support | DLQ access controlled; PII redacted in DLQ viewer | Check DLQ access policy |
| **DoS** | Poison message infinite retry | Max retries + exponential backoff + dead-letter | Check retry policy |
| **DoS** | Outbox table grows unbounded | Outbox agent polling + metrics + alerting on lag | Check outbox monitoring |
| **EoP** | Handler processes without restoring tenant context | `TenantContextBehavior` applied to handler pipeline | Check handler behavior registration |

---

## TM-08: GDPR Erasure on IoT Telemetry

**Data Flow:** Erasure request (data subject) → `GdprDeletionSaga` →
Discover all IoT data (devices owned, telemetry, timeline) → Delete or
crypto-shred → Audit → Confirmation

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Spoofing** | Unauthorized erasure request | Request authenticated + scoped to requester (data subject or admin) | Check erasure endpoint auth |
| **Tampering** | Partial deletion (telemetry deleted, device metadata kept) | Saga with compensation / atomic transaction across tables | Check saga boundaries |
| **Repudiation** | Organization denies deletion | Immutable audit entry via `ICryptoShreddingAuditRecorder` | Check audit immutability |
| **Info Disclosure** | Deleted telemetry recoverable from partitions | Partition drop or row-level delete + VACUUM | Check partition handling |
| **Info Disclosure** | Backup contains unshredded telemetry | Backup retention < GDPR deadline OR backup itself encrypted with rotatable keys | Check backup policy |
| **Info Disclosure** | Archived cold data not covered | IoT archival integrated with `IDataProviderRegistry` | Check registry coverage |
| **DoS** | Mass erasure triggers DB contention | Bounded concurrency, batched deletes | Check saga configuration |

---

## TM-09: Deserialization Across Ingestion Boundary

**Data Flow:** External provider payload (JSON) → Kestrel → `MapPost`
binding → Provider-specific parser → Domain DTO → Handler → PostgreSQL
(JSONB serializer)

Deserialization affects multiple components: Scaleway/AWS parsers, Wolverine
envelope, FusionCache (if used), Claim Check payloads, MCP tool responses,
JSONB telemetry column.

### STRIDE Analysis

| Threat | Vector | Mitigation (expected) | Verify |
|--------|--------|----------------------|--------|
| **Tampering** | Attacker injects `$type` discriminator to instantiate arbitrary types | `System.Text.Json` with closed `[JsonDerivedType]` sets; no `Newtonsoft.Json TypeNameHandling` | Grep for `TypeNameHandling` + polymorphic attrs |
| **Tampering** | Wolverine envelope contains crafted payload | Schema-first, known types only | Check Wolverine serializer |
| **Tampering** | JSONB column stores polymorphic type | JSONB serializer restricts types | Check EF JSONB value converter |
| **DoS** | Deeply nested JSON causes stack overflow | `MaxDepth` set to 64 (default) | Check global serializer options |
| **DoS** | 100 MB payload exhausts memory | Kestrel + Wolverine size limits | Check limits config |
| **DoS** | NaN / Infinity in numeric fields propagates | Parser rejects non-finite numbers | Check parser validation |
| **EoP** | Deserialized object's constructor executes code | DTOs are records with no constructor logic | Check DTO design |

### Attack Tree: RCE via Malicious Ingestion Payload

```text
Goal: Execute arbitrary code via crafted Scaleway/AWS webhook payload
├── [OR] Polymorphic JSON injection
│   ├── Newtonsoft.Json with TypeNameHandling != None
│   │   └── Check: grep for "Newtonsoft" — ideally absent
│   ├── System.Text.Json with open [JsonDerivedType] set
│   │   └── Check: all polymorphic types use closed discriminator list
│   └── Custom IJsonTypeInfoResolver
│       └── Check: custom resolvers restrict type set
├── [OR] Provider parser accepts unknown fields
│   ├── Unknown field triggers property setter with side effect
│   │   └── Check: DTOs are init-only records, no side effects in setters
│   └── Deep nesting bypasses validation
│       └── Check: MaxDepth + validator recursion limit
├── [OR] JSONB Metrics column
│   ├── Polymorphic payload stored and later deserialized
│   │   └── Check: Metrics serialized as plain JSON, no polymorphic types
└── [OR] Wolverine message forgery
    ├── Direct INSERT into outbox (via SQL injection elsewhere)
    │   └── Check: outbox table DB permissions (app user has limited INSERT)
    └── DLQ replay of crafted message
        └── Check: DLQ replay re-validates schema
```

---

## Methodology Notes

### Using these threat models

1. **Start with the relevant TM** for the domain being audited
2. **Verify each mitigation** exists in the actual code (Granit.IoT satellite)
3. **Follow attack trees** to find gaps
4. **Score findings** with CVSS 3.1
5. **Check compensating controls** before assigning final severity
6. **Update the TM** if new attack vectors are discovered

### Revision triggers — when to revisit a threat model

IoT threat models are especially sensitive to change because new cloud
provider integrations, new device classes, or new transport protocols can
introduce entire new attack surfaces.

| Change | Affected TMs | Why |
|--------|-------------|-----|
| New cloud provider (Azure IoT Hub, GCP IoT Core) | TM-01, TM-06, TM-09 | New signature scheme, new trust anchors |
| Self-hosted MQTT broker enabled / disabled | TM-05 | Broker presence/absence changes attack surface |
| Device shadow service added (Granit.IoT.Aws.Shadow) | TM-04, TM-05 | New read/write paths to device state |
| TimescaleDB introduced | TM-03, TM-08 | Partition/hypertable isolation semantics change |
| New MCP tool category (firmware OTA, device control) | TM-04 | New sensitive operations, new threat vectors |
| Notification publisher change (email, SMS, push) | TM-04 | New PII flow path, new spam / enumeration risks |
| Claim check pattern adopted for large telemetry | TM-07, TM-09 | Blob storage becomes trust boundary |
| Edge deployment / gateway introduced | All | Devices behind local gateway blur trust boundaries |

**Rule:** Any PR that modifies a trust boundary crossing should reference the
relevant TM and confirm mitigations still hold. The `/security diff` command
automates a portion of this check.

### CVSS 3.1 Quick Reference

| Metric | Values |
|--------|--------|
| Attack Vector (AV) | Network (N), Adjacent (A), Local (L), Physical (P) |
| Attack Complexity (AC) | Low (L), High (H) |
| Privileges Required (PR) | None (N), Low (L), High (H) |
| User Interaction (UI) | None (N), Required (R) |
| Scope (S) | Unchanged (U), Changed (C) |
| Confidentiality (C) | None (N), Low (L), High (H) |
| Integrity (I) | None (N), Low (L), High (H) |
| Availability (A) | None (N), Low (L), High (H) |

Example: `CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:N` = 10.0 (Critical)

**IoT-specific scoring tips:**

- Ingestion endpoint is `AV:N/PR:N` (public internet, no authentication
  required — the signature is the authentication, so a signature bypass is
  effectively `PR:N`).
- Cross-tenant leaks typically score `S:C` (scope changed: crossing a
  security authority boundary).
- Fleet-wide vulnerabilities often warrant escalation to Critical even with
  moderate per-request impact, because blast radius spans thousands of
  physical devices.
