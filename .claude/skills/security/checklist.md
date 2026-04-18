# Security Audit Checklist — Granit.IoT

Detailed verification matrix used by the `/security` skill. Each section
maps to a `<domain>` keyword. Apply methodically — check each item, note
evidence.

Standards referenced:

- **IoT** — OWASP IoT Top 10 (2018)
- **ASVS** — OWASP Application Security Verification Standard 4.0
- **API** — OWASP API Security Top 10 (2023)
- **LLM** — OWASP LLM Top 10 (2025)
- **ETSI** — ETSI EN 303 645 (Cyber Security for Consumer IoT)
- **NIST** — NIST IR 8259A (IoT Core Baseline)
- **ISO** — ISO 27001:2022 Annex A
- **GDPR** — General Data Protection Regulation
- **CWE** — Common Weakness Enumeration

---

## 1. Ingestion Pipeline (`ingestion`)

### 1a. Webhook Endpoint

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 1.1 | `/iot/ingest/{source}` authenticates via provider-specific signature BEFORE parsing payload | API2 | CRITICAL |
| 1.2 | `[AllowAnonymous]` on ingestion endpoint paired with provider signature validation (no bare anonymous acceptance) | API2 | CRITICAL |
| 1.3 | Kestrel `MaxRequestBodySize` bounded (prevent memory DoS) | API4 | HIGH |
| 1.4 | Wolverine envelope size bounded | API4 | HIGH |
| 1.5 | Response is always 202 Accepted (no oracle on device existence) | CWE-204 | MEDIUM |
| 1.6 | Response time is constant (no timing oracle on device validity) | CWE-208 | LOW |
| 1.7 | Error responses do not leak provider-specific failure reasons | CWE-209 | MEDIUM |

### 1b. Scaleway Provider Parser

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 1.8 | Scaleway webhook authentication (shared secret, JWT, or TLS mutual auth) enforced | IoT I3 | CRITICAL |
| 1.9 | Payload schema validated; unknown fields rejected OR explicitly ignored | CWE-20 | HIGH |
| 1.10 | Numeric fields bounded (prevent NaN/Infinity propagation to DB) | CWE-20 | MEDIUM |
| 1.11 | Scaleway project/region ID validated against tenant's configured Scaleway binding | CWE-346 | CRITICAL |
| 1.12 | Timestamp replay window bounded (< 5 min) | CWE-294 | HIGH |

### 1c. AWS Provider Parser

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 1.13 | `ISigV4RequestValidator` validates SigV4 signature with constant-time comparison | CWE-208 | CRITICAL |
| 1.14 | SigV4 `X-Amz-Date` within 5-minute skew (replay protection) | CWE-294 | CRITICAL |
| 1.15 | `SnsSigningCertificateCache` pins against known AWS SNS signing URL patterns (`*.amazonaws.com`) | CWE-295 | CRITICAL |
| 1.16 | SNS certificate chain validated against OS trust store | CWE-295 | CRITICAL |
| 1.17 | SNS cert cache has bounded TTL (not infinite) | CWE-613 | MEDIUM |
| 1.18 | AWS account ID in payload validated against tenant's configured AWS account | CWE-346 | CRITICAL |
| 1.19 | `ISigV4SigningKeyProvider` sources keys from Vault/KMS (not config) | ISO A.8.24 | CRITICAL |
| 1.20 | `SnsSigningCertFetchException` fails closed (does not accept payload on cert fetch failure) | CWE-636 | HIGH |

### 1d. Deduplication

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 1.21 | Dedup key includes tenant ID (prevent cross-tenant collision suppressing legitimate telemetry) | CWE-639 | CRITICAL |
| 1.22 | Dedup TTL bounded (5 min per CLAUDE.md) | API4 | LOW |
| 1.23 | Redis failure behavior documented (fail-closed vs fail-open trade-off) | CWE-636 | HIGH |
| 1.24 | Dedup survives leader failover (Redis replication / persistence) | API4 | MEDIUM |

### 1e. Wolverine Outbox Contract

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 1.25 | Ingestion endpoint writes outbox record in same DB transaction as telemetry | ASVS 8.3.7 | HIGH |
| 1.26 | `TelemetryIngestedHandler` is idempotent (at-least-once delivery) | CWE-400 | HIGH |
| 1.27 | DLQ messages scrubbed of PII before manual review | GDPR Art. 32 | MEDIUM |
| 1.28 | Poison message max retries + exponential backoff configured | CWE-400 | HIGH |
| 1.29 | Device existence + tenant binding verified in handler (not just at ingestion) | CWE-284 | CRITICAL |

---

## 2. Device Authentication (`device-auth`)

### 2a. X.509 Certificate Validation

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 2.1 | Certificate chain validated against tenant's configured root CA(s) | IoT I1, ETSI 5.1 | CRITICAL |
| 2.2 | Revocation checked (CRL / OCSP) with bounded timeout | IoT I1 | HIGH |
| 2.3 | `NotBefore` / `NotAfter` honored with clock skew tolerance <= 5 min | CWE-295 | HIGH |
| 2.4 | Subject CN or SAN matches expected device identifier format | CWE-295 | HIGH |
| 2.5 | Chain validation rejects SHA-1 / MD5 signatures | ASVS 6.2.5 | CRITICAL |
| 2.6 | No self-signed certs accepted in production (only during dev/test) | IoT I1 | CRITICAL |
| 2.7 | Private keys (if stored server-side) encrypted at rest — ideally NEVER stored server-side | ISO A.8.24 | CRITICAL |

### 2b. Fleet Provisioning (AWS)

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 2.8 | `Granit.IoT.Aws.FleetProvisioning` template scoped per tenant (no shared template) | CWE-284 | CRITICAL |
| 2.9 | Bootstrap credentials (claim certs) short-lived and different from operational creds | ETSI 5.1 | HIGH |
| 2.10 | Provisioned device policy follows least privilege (publish/subscribe only on own topics) | CWE-250 | HIGH |
| 2.11 | Race between `CreateCertificateFromCsr` and Granit device binding handled atomically — if binding fails, cert revoked | CWE-362 | HIGH |
| 2.12 | CSR validation rejects weak keys (<2048 RSA, <P-256 ECDSA) | NIST SP 800-57 | HIGH |
| 2.13 | Registration tokens >= 128 bits of entropy | CWE-330 | HIGH |
| 2.14 | Fleet provisioning audit trail (who/what/when/device-id/tenant) | ISO A.8.15 | HIGH |

### 2c. Device-Tenant Binding

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 2.15 | Device-tenant binding is immutable once established | CWE-284 | CRITICAL |
| 2.16 | Binding cannot be spoofed via payload field (e.g., `tenantId` in telemetry always ignored in favor of server-side binding) | CWE-639 | CRITICAL |
| 2.17 | Orphaned devices (cert valid but no binding) rejected at ingestion | CWE-284 | HIGH |
| 2.18 | Revocation is immediate (cache invalidation on revoke) | ETSI 5.3 | HIGH |
| 2.19 | Revoked devices require admin approval to re-register | CWE-284 | MEDIUM |

---

## 3. MQTT Transport (`mqtt`)

### 3a. TLS & Authentication

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 3.1 | TLS 1.2 minimum, 1.3 preferred | ASVS 9.1.2 | CRITICAL |
| 3.2 | Cipher suites restricted (no RC4, no CBC without HMAC) | ASVS 9.2.3 | HIGH |
| 3.3 | Client certificate authentication mandatory (no username/password alone) | IoT I1 | CRITICAL |
| 3.4 | Client cert validated per §2a rules (chain, revocation, not-after) | IoT I1 | CRITICAL |
| 3.5 | `MQTTnet` broker version pinned and CVE-clean | IoT I5 | HIGH |

### 3b. Topic-Level Authorization

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 3.6 | Topic ACL enforces `tenants/{tenantId}/devices/{deviceId}/#` scope | CWE-639 | CRITICAL |
| 3.7 | `ValidatingConnectionAsync` hook verifies client cert matches device identity | CWE-287 | CRITICAL |
| 3.8 | `InterceptingPublishAsync` hook verifies topic ownership before routing | CWE-639 | CRITICAL |
| 3.9 | `InterceptingSubscriptionAsync` hook verifies subscription scope | CWE-639 | CRITICAL |
| 3.10 | Broadcast topics (if any) explicitly enumerated, not wildcarded | CWE-639 | HIGH |

### 3c. Resource Exhaustion

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 3.11 | Max payload size bounded (MQTTnet default 256 MB is too high — enforce tenant-scoped limit) | API4 | HIGH |
| 3.12 | QoS 2 in-flight limit per device (prevent memory exhaustion via unacked messages) | API4 | MEDIUM |
| 3.13 | Per-device publish rate limit | API4 | HIGH |
| 3.14 | Retained messages disabled OR scoped per tenant with size/count cap | CWE-400 | MEDIUM |
| 3.15 | Will messages (LWT) validated against topic ACL | CWE-639 | MEDIUM |

---

## 4. Cloud Provider Integrations (`cloud-providers`)

### 4a. AWS IoT Core

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 4.1 | IAM role for Granit IoT limited to minimal actions (no `iot:*` wildcard) | CWE-250 | CRITICAL |
| 4.2 | No long-lived AWS access keys — use OIDC federation / instance role | ISO A.5.17 | CRITICAL |
| 4.3 | Cross-account access via AssumeRole with `sts:ExternalId` | CWE-346 | HIGH |
| 4.4 | `AWSSDK` packages pinned to CVE-clean versions | IoT I5 | HIGH |
| 4.5 | AWS region allowlist per tenant (data residency) | GDPR Art. 44 | HIGH |
| 4.6 | Ingestion validates payload AWS account ID matches tenant's bound account | CWE-346 | CRITICAL |

### 4b. Scaleway IoT Hub

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 4.7 | Scaleway API keys stored in Vault / secret manager | ISO A.8.24 | CRITICAL |
| 4.8 | Key rotation automated (< 90 days) | ISO A.8.24 | HIGH |
| 4.9 | Scaleway webhook authenticated (shared secret, JWT, or mTLS) | IoT I3 | CRITICAL |
| 4.10 | Scaleway region allowlist per tenant (data residency — `fr-par` for EU) | GDPR Art. 44 | HIGH |
| 4.11 | Scaleway project ID validated against tenant's binding | CWE-346 | CRITICAL |

### 4c. Outbound Credentials

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 4.12 | No hardcoded credentials in any `appsettings*.json` | CWE-798 | CRITICAL |
| 4.13 | Credentials zeroized from memory after use | CWE-244 | MEDIUM |
| 4.14 | Credential provider logs sanitized (no key material in exceptions) | CWE-532 | HIGH |

---

## 5. Multi-Tenancy Isolation (`tenancy`)

### 5a. Device-Tenant Binding

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 5.1 | `Device` entity implements `IMultiTenant` | CWE-639 | CRITICAL |
| 5.2 | `TenantId` property has `private set` — set by interceptor only | CWE-639 | CRITICAL |
| 5.3 | Telemetry entities inherit tenant from device (authoritative), not from payload | CWE-639 | CRITICAL |
| 5.4 | Device lookup by external ID always scoped by tenant | CWE-639 | CRITICAL |

### 5b. Query Filters

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 5.5 | `ApplyGranitConventions` applied to all IoT entities (Device, Telemetry, Timeline, Credentials) | CWE-639 | CRITICAL |
| 5.6 | Every `IgnoreQueryFilters()` occurrence justified and audit-logged | CWE-639 | HIGH |
| 5.7 | `ExecuteUpdate`/`ExecuteDelete` bulk operations manually include `.Where(e => e.TenantId == tenantId)` | CWE-639 | CRITICAL |
| 5.8 | JSONB queries on `Metrics` column preceded by tenant filter | CWE-639 | HIGH |
| 5.9 | TimescaleDB hypertables (if used) apply tenant filter (RLS or framework filter) | CWE-639 | CRITICAL |

### 5c. Cross-Service Isolation

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 5.10 | Cache keys (device state, shadow) prefixed with tenant ID | CWE-639 | CRITICAL |
| 5.11 | Wolverine `TenantContextBehavior` applied to ingestion pipeline | CWE-639 | CRITICAL |
| 5.12 | Background jobs (purge, heartbeat) restore tenant context per iteration | CWE-639 | HIGH |
| 5.13 | Notification dispatch scoped to device's tenant | CWE-639 | HIGH |
| 5.14 | MCP tool responses scoped by `McpTenantScopeAttribute` | CWE-639 | CRITICAL |
| 5.15 | Rate limit partitioned by tenant (`TenantPartitionedRateLimiter`) | CWE-639 | HIGH |

### 5d. Partitioning

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 5.16 | Monthly telemetry partitions respect tenant boundaries | CWE-639 | MEDIUM |
| 5.17 | Partition pruning during queries includes tenant filter | CWE-639 | HIGH |

---

## 6. Data Protection & GDPR (`data`)

### 6a. Telemetry as PII

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 6.1 | `[SensitiveData]` applied to telemetry fields relating to natural persons (geolocation, health metrics) | GDPR Art. 5 | CRITICAL |
| 6.2 | GPS coordinates flagged `Confidential` or `Restricted` | GDPR Art. 5 | HIGH |
| 6.3 | Device owner identifiers (user ID, employee ID) flagged as PII | GDPR Art. 5 | HIGH |
| 6.4 | `SensitivePropertyRegistry` auto-feeds MCP/audit redaction for IoT entities | LLM06 | HIGH |

### 6b. Encryption at Rest

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 6.5 | Database-level TDE OR column-level encryption for sensitive JSONB fields | GDPR Art. 32 | HIGH |
| 6.6 | Device credentials (if stored server-side) encrypted with per-device key | GDPR Art. 32 | CRITICAL |
| 6.7 | Backup data encrypted at rest | GDPR Art. 32 | HIGH |
| 6.8 | Cache (Redis) uses TLS and AUTH | GDPR Art. 32 | HIGH |

### 6c. GDPR Rights

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 6.9 | `GdprDeletionSaga` covers telemetry, device metadata, timeline, notifications | GDPR Art. 17 | CRITICAL |
| 6.10 | IoT module registered in `IDataProviderRegistry` for exports | GDPR Art. 20 | HIGH |
| 6.11 | Telemetry export format machine-readable (JSON/CSV) | GDPR Art. 20 | MEDIUM |
| 6.12 | Deletion completes within 30-day SLA | GDPR Art. 12 | MEDIUM |
| 6.13 | Deletion audit trail immutable | ISO A.8.15 | HIGH |

### 6d. Retention & Minimization

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 6.14 | Telemetry retention configurable per tenant with enforced ceiling | GDPR Art. 5 | HIGH |
| 6.15 | `Granit.IoT.BackgroundJobs` purge job enforces retention (not just documented) | GDPR Art. 5 | CRITICAL |
| 6.16 | Purge job failures alert (missed purge = retention violation) | ISO A.8.15 | HIGH |
| 6.17 | Archival to cold storage preserves tenant isolation and encryption | GDPR Art. 32 | MEDIUM |

---

## 7. AI & MCP (`ai`)

### 7a. Tool Visibility

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 7.1 | All tools in `Granit.IoT.Mcp/Tools` opt-in via `[McpExposed]` (explicit discovery) | LLM07 | CRITICAL |
| 7.2 | `TenantAwareVisibilityFilter` enforces tenant isolation on tool discovery | LLM06 | CRITICAL |
| 7.3 | Sensitive tools (fleet provisioning, device deletion, cert revocation) require elevated permissions | LLM08 | CRITICAL |
| 7.4 | Tool descriptions do not leak internal architecture (broker URLs, table names) | LLM06 | MEDIUM |

### 7b. Output Sanitization

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 7.5 | `Granit.IoT.Mcp/Responses` DTOs apply `[SensitiveData]` on PII fields (geolocation, owner IDs) | LLM06 | CRITICAL |
| 7.6 | Bulk responses paginated / capped (no full-fleet dump in single response) | LLM06 | HIGH |
| 7.7 | Error responses sanitized (no connection strings, no internal IDs) | LLM06 | HIGH |
| 7.8 | Telemetry responses scoped to calling tenant | LLM06 | CRITICAL |

### 7c. Prompt Injection

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 7.9 | Device names / tags validated (length, charset) before inclusion in tool responses | LLM01 | HIGH |
| 7.10 | User-controlled strings not interpolated into tool descriptions or schemas | LLM01 | CRITICAL |
| 7.11 | Tool parameter schemas validate input types and ranges | LLM01 | HIGH |

### 7d. Confused Deputy & Authorization

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 7.12 | Tool execution checks CALLING user's permissions (not tool owner) | LLM08 | CRITICAL |
| 7.13 | `McpTenantScopeAttribute` enforced on every IoT tool | LLM08 | CRITICAL |
| 7.14 | MCP tool invocations audited (who, what, when, tenant, affected devices) | LLM07 | HIGH |
| 7.15 | Tool chaining cannot escalate privileges across calls | LLM08 | HIGH |

### 7e. Resource Exhaustion

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 7.16 | MCP tool calls rate-limited per user/tenant | LLM08 | HIGH |
| 7.17 | Expensive queries (bulk telemetry export) have timeouts and result-set caps | LLM08 | HIGH |
| 7.18 | Recursive tool chains bounded (max depth) | LLM08 | HIGH |

---

## 8. Infrastructure & Resilience (`infra`)

### 8a. Wolverine

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 8.1 | Outbox writes in same DB transaction as business data | ASVS 8.3.7 | HIGH |
| 8.2 | Handlers idempotent (at-least-once semantics) | CWE-400 | HIGH |
| 8.3 | Max retries + exponential backoff configured | CWE-400 | HIGH |
| 8.4 | DLQ access controlled; messages scrubbed of PII before review | GDPR Art. 32 | HIGH |
| 8.5 | W3C Trace Context propagated across ingestion pipeline | ISO A.8.15 | MEDIUM |
| 8.6 | `TenantContextBehavior` applied to async handlers | CWE-639 | CRITICAL |
| 8.7 | Message envelope uses schema-first deserialization | CWE-502 | CRITICAL |

### 8b. Rate Limiting

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 8.8 | Rate limiting applied BEFORE signature validation (cheap DoS protection) | API4 | HIGH |
| 8.9 | Per-device rate limit (prevent compromised device flooding) | API4 | HIGH |
| 8.10 | Per-tenant rate limit (prevent noisy neighbor) | API4 | HIGH |
| 8.11 | `CounterStoreFailureBehavior` is CLOSED (deny on Redis failure) | CWE-636 | HIGH |
| 8.12 | Rate limit headers returned (`Retry-After`) | ASVS 13.1.5 | LOW |

### 8c. Idempotency

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 8.13 | Dedup key = tenant + device + message ID (prevent cross-tenant collision) | CWE-639 | CRITICAL |
| 8.14 | Idempotency window bounded (TTL prevents indefinite storage) | CWE-400 | MEDIUM |
| 8.15 | Idempotency store tenant-partitioned | CWE-639 | HIGH |

### 8d. Background Jobs

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 8.16 | Telemetry purge job bounded batch size | CWE-400 | MEDIUM |
| 8.17 | Heartbeat timeout job tenant-scoped (no global scan) | CWE-639 | HIGH |
| 8.18 | Job failures alert (missed purge = retention violation) | ISO A.8.15 | HIGH |
| 8.19 | Jobs run with minimal DB permissions (no DDL, no superuser) | CWE-250 | HIGH |

---

## 9. Supply Chain (`supply-chain`)

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 9.1 | `packages.lock.json` present for all projects | CWE-1357 | HIGH |
| 9.2 | No known CVEs (`dotnet list package --vulnerable`) | IoT I5 | CRITICAL |
| 9.3 | No deprecated packages (`dotnet list package --deprecated`) | IoT I5 | MEDIUM |
| 9.4 | `AWSSDK.*` packages pinned to CVE-clean versions | IoT I5 | HIGH |
| 9.5 | `MQTTnet` pinned to CVE-clean version | IoT I5 | HIGH |
| 9.6 | Scaleway SDK pinned to exact version (audit carefully, less mature) | IoT I5 | HIGH |
| 9.7 | Non-permissive licenses flagged (GPL, LGPL, AGPL, SSPL) | Legal | HIGH |
| 9.8 | `THIRD-PARTY-NOTICES.md` matches actual dependency tree | Legal | MEDIUM |
| 9.9 | No prerelease packages in Release config | CWE-1357 | MEDIUM |
| 9.10 | GitHub Actions workflows use OIDC federation to AWS (no long-lived secrets) | ISO A.5.17 | CRITICAL |
| 9.11 | NuGet packages signed (published Granit.IoT packages) | CWE-1357 | HIGH |
| 9.12 | Pre-commit secret scanning enabled (gitleaks / trufflehog) | CWE-798 | HIGH |
| 9.13 | CI pipeline runs secret scanning on every push | CWE-798 | HIGH |
| 9.14 | Historical AWS/Scaleway secrets in git history identified and rotated | CWE-798 | HIGH |

---

## 10. Cryptographic Correctness (`crypto`)

### 10a. Algorithm Inventory

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 10.1 | No MD5 / SHA-1 for security purposes | ASVS 6.2.5 | CRITICAL |
| 10.2 | AES uses GCM (authenticated encryption); never ECB or unauthenticated CBC | ASVS 6.2.1 | CRITICAL |
| 10.3 | RSA key size >= 2048 bits (device certs accepted) | NIST SP 800-57 | HIGH |
| 10.4 | ECDSA uses P-256 or P-384 curves | NIST SP 800-57 | HIGH |
| 10.5 | HMAC-SHA256 minimum for signature validation (SigV4, webhooks) | ASVS 6.2.5 | HIGH |

### 10b. Certificate Handling

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 10.6 | X.509 chain validation against pinned tenant root CAs | CWE-295 | CRITICAL |
| 10.7 | Certificate revocation (CRL / OCSP) checked with timeout | CWE-295 | HIGH |
| 10.8 | SNS signing cert pinned by thumbprint and URL pattern | CWE-295 | CRITICAL |
| 10.9 | Certificate expiry monitored (alerts before expiration) | ETSI 5.3 | HIGH |

### 10c. Random & Nonce

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 10.10 | `RandomNumberGenerator` used; never `System.Random` for security | CWE-330 | CRITICAL |
| 10.11 | Fleet provisioning registration tokens >= 128 bits entropy | ASVS 2.4.1 | HIGH |
| 10.12 | SigV4 timestamps / nonces non-repeating within skew window | CWE-294 | HIGH |

### 10d. Constant-Time Comparison

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 10.13 | HMAC / signature comparison uses `CryptographicOperations.FixedTimeEquals` | CWE-208 | CRITICAL |
| 10.14 | Device credential comparison is timing-safe | CWE-208 | HIGH |

### 10e. Key Lifecycle

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 10.15 | Keys never logged, never in error messages | CWE-532 | CRITICAL |
| 10.16 | Key material zeroized after use (`CryptographicOperations.ZeroMemory`) | CWE-244 | HIGH |
| 10.17 | Key rotation automated (signing keys, webhook secrets) | ISO A.8.24 | HIGH |

---

## 11. Observability Security (`observability`)

### 11a. Logging

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 11.1 | No hardcoded secrets in `appsettings*.json` (connection strings, API keys, webhook secrets) | CWE-798 | CRITICAL |
| 11.2 | No PII in log messages (owner names, emails, geolocation) | GDPR Art. 5 | CRITICAL |
| 11.3 | No device private keys / credentials in logs | CWE-532 | CRITICAL |
| 11.4 | `.gitignore` excludes sensitive files (`.env`, `*.pfx`, `*.key`, `*.pem`, `aws-credentials`) | CWE-798 | HIGH |
| 11.5 | Telemetry payloads NOT logged in full (only shape / schema) | GDPR Art. 5 | HIGH |
| 11.6 | Structured logging redacts `[SensitiveData]`-flagged fields | GDPR Art. 5 | HIGH |
| 11.7 | Exception details sanitized in production (no stack traces to client) | CWE-209 | HIGH |
| 11.8 | Log injection prevented (user input not directly in log templates) | CWE-117 | MEDIUM |

### 11b. Metrics

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 11.9 | Device IDs NOT used as metric tag values (cardinality explosion) | API4 | CRITICAL |
| 11.10 | Tenant ID acceptable as tag (bounded cardinality) | API4 | INFO |
| 11.11 | Provider (`aws`, `scaleway`) acceptable as tag | API4 | INFO |
| 11.12 | Metric names do not reveal internal architecture | CWE-200 | LOW |
| 11.13 | `AwsIoTIngestionMetrics` reviewed for high-cardinality tags | API4 | HIGH |

### 11c. Distributed Tracing

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 11.14 | W3C Trace Context NOT propagated outbound to cloud provider APIs | CWE-200 | MEDIUM |
| 11.15 | Span attributes do not contain raw telemetry payloads | GDPR Art. 5 | HIGH |
| 11.16 | Span attributes do not contain device secrets or credentials | CWE-532 | CRITICAL |

### 11d. Timeline / Audit

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 11.17 | `Granit.IoT.Timeline` append-only (no update/delete) | ISO A.8.15 | CRITICAL |
| 11.18 | Device lifecycle events (provisioning, decommissioning, revocation) audited | ISO A.8.15 | HIGH |
| 11.19 | Timeline retention meets regulatory requirements (>= 1 year) | ISO A.8.15 | MEDIUM |

---

## 12. Deserialization Safety (`deserialization`)

### 12a. JSON Deserialization

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 12.1 | `System.Text.Json` used; no `Newtonsoft.Json` with `TypeNameHandling` | CWE-502 | CRITICAL |
| 12.2 | No `JsonSerializerOptions.TypeInfoResolver` that resolves arbitrary types | CWE-502 | CRITICAL |
| 12.3 | Polymorphic deserialization uses `[JsonDerivedType]` with closed type set | CWE-502 | HIGH |
| 12.4 | `JsonSerializerOptions.MaxDepth` not loosened above default 64 | CWE-400 | MEDIUM |

### 12b. Provider Parsers

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 12.5 | Scaleway parser validates schema before processing | CWE-20 | HIGH |
| 12.6 | AWS SNS parser validates schema before processing | CWE-20 | HIGH |
| 12.7 | Unknown fields rejected or explicitly ignored (policy documented) | CWE-20 | MEDIUM |
| 12.8 | Numeric field ranges enforced (reject NaN, Infinity, out-of-range) | CWE-20 | MEDIUM |

### 12c. JSONB (Telemetry Metrics Column)

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 12.9 | JSONB deserializer does not resolve arbitrary types | CWE-502 | CRITICAL |
| 12.10 | GIN index on `Metrics` does not bypass tenant query filter | CWE-639 | CRITICAL |

### 12d. Wolverine Envelope

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 12.11 | Message envelope uses schema-first deserialization (known types only) | CWE-502 | CRITICAL |
| 12.12 | DLQ replay validates message schema before re-processing | CWE-502 | HIGH |
| 12.13 | No `BinaryFormatter`, no `NetDataContractSerializer` | CWE-502 | CRITICAL |

### 12e. MCP Tool Responses

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 12.14 | MCP tool output uses closed type set | CWE-502 | HIGH |
| 12.15 | Structured content validated against schema | CWE-502 | HIGH |

---

## 13. Cross-Cutting Concerns

These checks apply across all domains:

| # | Check | Standard | Severity if missing |
|---|-------|----------|---------------------|
| 13.1 | No hardcoded secrets in any file (code, config, tests) | CWE-798 | CRITICAL |
| 13.2 | All external HTTP calls use HTTPS (cloud provider APIs, webhooks) | ASVS 9.1.1 | HIGH |
| 13.3 | Content-Type validation on ingestion endpoints | CWE-20 | MEDIUM |
| 13.4 | Request size limits configured (prevent large payload DoS) | API4 | HIGH |
| 13.5 | All `async` methods accept and forward `CancellationToken` | CWE-400 | LOW |
| 13.6 | Error responses use RFC 7807 Problem Details (no internal data leak) | CWE-209 | MEDIUM |
| 13.7 | Architecture tests cover security conventions (query filter coverage, sensitive-data annotations) | ISO A.8.25 | MEDIUM |
| 13.8 | `Granit.IoT.ArchitectureTests` asserts every sub-package module — including security modules | ISO A.8.25 | MEDIUM |
