# Granit.IoT — Audit Checklist

Verification matrix used by the `/audit` skill. Each category maps to a
`--scope` value. Apply in order.

Convention references point to:

- `CLAUDE.md` — project conventions (root of this repo)
- `granit-tools::docs_get:<id>` — Granit framework doc (lives in `granit-dotnet`)
- `GRXXXX` — Roslyn analyzer rule shipped by the framework

Whenever you cite a framework rule, **verify it via the Granit MCP** first
(`docs_search` then `docs_get`). Do not rely on memory.

---

## 1. Module anatomy (`--scope anatomy`)

### 1a. Ring discipline (repo-specific)

The IoT module family is split across 3 cohesion rings (see CLAUDE.md §Architecture):

| Ring | Purpose | Projects |
| ------ | --------- | ---------- |
| 1 | Core device management | `Granit.IoT`, `.Endpoints`, `.EntityFrameworkCore*`, `.BackgroundJobs` |
| 2 | Telemetry ingestion | `Granit.IoT.Ingestion*`, `Granit.IoT.Wolverine` |
| 3 | Extensions / bridges | `Granit.IoT.Notifications`, `.Timeline`, `.AI.Mcp`, `bundles/` |

- [ ] Ring 1 projects have no `<ProjectReference>` to Ring 2 or Ring 3 projects
- [ ] Ring 2 projects have no `<ProjectReference>` to Ring 3 projects
- [ ] Ring 3 projects may reference any lower ring
- [ ] `bundles/Granit.Bundle.IoT` is a pure meta-package (no code, only
  `<ProjectReference>` to satellites)

### 1b. Layered split

- [ ] Base project `Granit.IoT` exists with interfaces, options, DI extension
- [ ] Module class inherits `GranitModule` and is `public sealed`
- [ ] `[DependsOn]` attributes match actual `<ProjectReference>` graph (direct
  only, transitive omitted, alphabetical order)
- [ ] `Granit` is never listed in `[DependsOn]` (implicit base)
- [ ] Zero-dependency satellites have no `[DependsOn]` attribute
- [ ] `.Endpoints` project exists for any satellite exposing HTTP API
- [ ] `.EntityFrameworkCore` project exists for any satellite with persistence
- [ ] Provider projects (`.Scaleway`, future `.Mqtt`, etc.) implement
  abstractions only — no cross-provider references

Ref: `docs_search` → "module system"

### 1c. Project structure

- [ ] File-scoped namespaces (`namespace X;`) — no brace-wrapped namespaces
- [ ] Namespace matches project name (e.g. `namespace Granit.IoT.Ingestion;`)
- [ ] One primary type per file (file name = type name)
- [ ] `Internal/` folder for non-public implementation types
- [ ] `Diagnostics/` folder for metrics and activity source (if applicable)
- [ ] `Jobs/` folder for background jobs (if applicable)
- [ ] No cross-project `internal` references (GRMOD001 — compile error)

Ref: `CLAUDE.md §Architecture`, `docs_search` → "analyzers"

### 1d. DI registration

- [ ] `AddIoT*(this IServiceCollection)` extension method exists per satellite
  that owns services
- [ ] Extension method is in `Microsoft.Extensions.DependencyInjection` namespace
- [ ] `TryAdd*` used (not `Add*`) to allow consumer overrides
- [ ] `GranitActivitySourceRegistry.Register(Name)` called if the satellite has
  tracing
- [ ] No extension method parameters for config (use `appsettings.json` /
  Vault) — exception: `Action<DbContextOptionsBuilder>` for EF Core
- [ ] Correct lifetime: Singleton for stateless, Scoped for request/user/tenant,
  Transient only for lightweight disposable types

Ref: `docs_search` → "dependency injection"

### 1e. Options pattern

- [ ] Options class is `sealed class` with `const string SectionName`
- [ ] Bound via `BindConfiguration(SectionName)`
- [ ] `ValidateDataAnnotations()` for constraint validation
- [ ] `ValidateOnStart()` for fail-fast on misconfiguration
- [ ] No secrets in `appsettings.json` — use env vars, User Secrets, or Vault

Ref: `docs_search` → "configuration"

---

## 2. Code conventions (`--scope code`)

### 2a. C# 14 / .NET 10 features

- [ ] Primary constructors for DI service classes (private readonly fields when
  param used in multiple methods)
- [ ] Collection expressions: `[x, y]` not `new[] { x, y }`, `[]` not
  `Array.Empty<T>()` or `new List<T>()`
- [ ] `System.Threading.Lock` for synchronization — never `lock(object)` or
  `lock(this)`
- [ ] `params ReadOnlySpan<T>` over `params T[]` in non-attribute methods
- [ ] Named query filters: `HasQueryFilter(name, expr)` — never unnamed
- [ ] Pattern matching preferred: `is`, `switch` expressions, list/property
  patterns

### 2b. Mandatory patterns

- [ ] `var` when type is apparent; explicit type otherwise
- [ ] Expression body (`=>`) for single-statement methods
- [ ] String interpolation `$"..."` — not `string.Concat` / `string.Format`
- [ ] `[GeneratedRegex]` — never `new Regex(..., Compiled)` — timeout on user input
- [ ] `[LoggerMessage]` — never string interpolation in log calls
- [ ] `TimeProvider` / `IClock` — never `DateTime.Now` / `UtcNow` (GRSEC001)
- [ ] `IGuidGenerator` — never `Guid.NewGuid()` (GRSEC002)
- [ ] `ConfigureAwait(false)` in library code
- [ ] `CancellationToken` as last parameter in async methods
- [ ] `ArgumentNullException.ThrowIfNull()` — not manual null checks
- [ ] `ArgumentException.ThrowIfNullOrEmpty()` / `ThrowIfNullOrWhiteSpace()`
  for strings
- [ ] `TypedResults.*()` — never `Results.*()` (GRAPI001)
- [ ] `IHttpClientFactory` — never `new HttpClient()`
- [ ] `IMeterFactory` — never `new Meter(...)`
- [ ] `IGranitCookieManager` — never direct `IResponseCookies` (GRSEC004)
- [ ] `SaveChangesAsync()` — never `SaveChanges()` (GREF001)
- [ ] No `async void` — always return `Task`
- [ ] No `.Result` / `.Wait()` — always `await`
- [ ] No bare `catch (Exception)` — catch specific types
- [ ] No hardcoded secrets, even in comments (GRSEC003)

Ref: `CLAUDE.md`, `docs_search` → "analyzers"

### 2c. Anti-patterns (flag if found)

- [ ] `*Dto` suffix on types (should be `*Request` / `*Response`)
- [ ] `TypedResults.BadRequest<string>()` (should be `TypedResults.Problem()`)
  (GRAPI002)
- [ ] EF entities returned from endpoints (must map to `*Response` DTOs)
- [ ] `Results.Ok()` instead of `TypedResults.Ok()`
- [ ] `new Meter(...)` instead of `IMeterFactory`
- [ ] Combined `I*Store` interfaces (should be separate `I*Reader` / `I*Writer`)
- [ ] Repository pattern over EF Core
- [ ] Cross-satellite direct method calls that skip integration events
- [ ] `Guid.NewGuid()` instead of `IGuidGenerator`
- [ ] `DateTime.Now`/`UtcNow` instead of `IClock`/`TimeProvider`
- [ ] Symbols listed in `BannedSymbols.txt` — these should already be blocked
  at compile time; flag if suppressed

---

## 3. Naming conventions (`--scope naming`)

### 3a. Permissions

- [ ] Three-segment format: `IoT.{Resource}.{Action}`
- [ ] Group matches `IoTPermissions.GroupName` (`"IoT"`, PascalCase)
- [ ] Resource is a plural noun nested static class (`Devices`, `Telemetry`,
  `Thresholds`)
- [ ] Action uses standard verbs: `Read`, `Create`, `Update`, `Delete`,
  `Manage`, `Execute`, `Ingest`
- [ ] Never `View` — always `Read`
- [ ] Permission constant: `public const string Read = "IoT.{Resource}.Read";`
- [ ] Localization keys: `PermissionGroup:IoT` and
  `Permission:IoT.{Resource}.{Action}`
- [ ] Provider class: `internal sealed class IoTPermissionDefinitionProvider
  : IPermissionDefinitionProvider`
- [ ] Permissions attached to roles only — never user-level grants (RBAC strict)

Ref: `CLAUDE.md`, `docs_search` → "security model"

### 3b. Events

- [ ] Domain events implement `IDomainEvent` with `*Event` suffix
  (e.g. `DeviceRegisteredEvent`, `TelemetryIngestedEvent`,
  `ThresholdBreachedEvent`)
- [ ] Integration events implement `IIntegrationEvent` with `*Eto` suffix
  (e.g. `DeviceRegisteredEto`, `ThresholdBreachedEto`)
- [ ] Past-tense verb in event name
- [ ] No bare past-tense names without suffix
- [ ] Generic lifecycle events: `EntityCreatedEvent<Device>` /
  `EntityCreatedEto<Device>` via `IEmitEntityLifecycleEvents` marker

Ref: `docs_search` → "entity lifecycle events"

### 3c. Background jobs

- [ ] `*Job` suffix on job records
- [ ] `sealed record` implementing `IBackgroundJob`
- [ ] `[RecurringJob("cron", "name")]` attribute
- [ ] Job name format: `iot-{action-kebab}`
  (e.g. `iot-telemetry-purge`, `iot-heartbeat-timeout`)
- [ ] Handler: `{Action}Handler` as `internal static partial class`
- [ ] Located in `Granit.IoT.BackgroundJobs/Jobs/` — never inside a Wolverine
  handler project

### 3d. DTOs and API types

- [ ] Prefixed to avoid OpenAPI collisions: `IngestTelemetryRequest`,
  `DeviceRegistrationRequest` — not `TelemetryRequest`
- [ ] `*Request` suffix for input DTOs
- [ ] `*Response` suffix for output DTOs
- [ ] Never `*Dto` suffix
- [ ] Enum JSON serialization: PascalCase via `JsonStringEnumConverter`
- [ ] Dates: ISO 8601 with timezone (`2025-03-15T10:30:00Z`)
- [ ] Identifiers: UUID v7 with dashes

Ref: `docs_search` → "http conventions"

### 3e. Module naming homogeneity (post-rename check)

The canonical module name is `IoT`. All artifacts must use it consistently.

**Classes — must embed `IoT`:**

- [ ] Module class: `GranitIoTModule`
- [ ] DbContext: `IoTDbContext`
- [ ] ModelBuilder extensions: `IoTModelBuilderExtensions`
- [ ] ServiceCollection extensions: `IoTServiceCollectionExtensions`
- [ ] Options classes: `IoT{Feature}Options` (e.g. `IoTIngestionOptions`)
- [ ] Metrics class: `IoTMetrics` (+ `IoTIngestionMetrics` etc. per satellite)
- [ ] ActivitySource class: `IoTActivitySource` (+ satellite variants)
- [ ] Permission class: `IoTPermissions`
- [ ] Permission provider: `IoTPermissionDefinitionProvider`
- [ ] Localization resource: `IoTEndpointsLocalizationResource`
- [ ] Health check: `IoTHealthCheck` (if applicable)

**Methods — must embed `IoT`:**

- [ ] DI registration: `AddIoT()`, `AddGranitIoT()`, `AddIoTIngestion()` etc.
- [ ] EF model config: `ConfigureIoTModule()`
- [ ] Endpoint mapping: `MapIoTEndpoints()`, `MapIoTIngestionEndpoints()`

**String literals — must use current module name:**

- [ ] Meter name: `"Granit.IoT"` (and satellite-specific where applicable)
- [ ] ActivitySource name: `"Granit.IoT"`
- [ ] Job name prefix: `"iot-"` (kebab-case)
- [ ] Permission group name: `"IoT"` (PascalCase)
- [ ] Log categories / event names

**Namespaces — must match project name:**

- [ ] All `.cs` in `Granit.IoT` use `namespace Granit.IoT;` (or sub-namespace)
- [ ] All `.cs` in `Granit.IoT.Endpoints` use `namespace Granit.IoT.Endpoints;`
- [ ] All `.cs` in `Granit.IoT.Ingestion` use `namespace Granit.IoT.Ingestion;`
- [ ] All `.cs` in `Granit.IoT.Wolverine` use `namespace Granit.IoT.Wolverine;`
- [ ] Every satellite follows the same pattern

**Detection strategy:**

1. Grep all `.cs` files for a name mismatch
2. Check recent renames: `git log --diff-filter=R --name-status -20`
3. Flag any class, method, literal, or namespace still using a previous name

---

## 4. HTTP conventions (`--scope http`)

### 4a. Status codes

- [ ] `TypedResults.Problem()` for all errors (RFC 7807) — never
  `BadRequest<string>()`
- [ ] `202 Accepted` for async operations — response includes tracking mechanism
  (requestId, Location header, or webhook). **Required for ingestion endpoints**
  (`POST /iot/ingest/{source}` — see CLAUDE.md §Key design decisions)
- [ ] `204 No Content` for DELETE, PUT /settings, acknowledgments
- [ ] `207 Multi-Status` for batch operations with mixed results only
- [ ] `409 Conflict` for concurrency conflicts (stale version, duplicate)
- [ ] `422 Unprocessable Entity` for business validation failures
  (FluentValidation)

Ref: `docs_search` → "http conventions"

### 4b. URL structure

- [ ] Resource segments in `kebab-case` (`/devices`, `/iot/ingest/scaleway`)
- [ ] Route parameters in `camelCase` (`{deviceId}`)
- [ ] Route groups use `MapGranitGroup(prefix)` — not `MapGroup()`

### 4c. Pagination

- [ ] Offset pagination (default): `?page=1&pageSize=20&sort=-recordedAt`
- [ ] Cursor pagination (opt-in): `?cursor=<opaque>&pageSize=50`
  — required for telemetry history endpoints
- [ ] Response via `PagedResult<T>` with `Items`, `TotalCount`, `HasMore`,
  `NextCursor`
- [ ] Sort format: comma-separated, `-` prefix for descending

### 4d. Filtering

- [ ] Field filters: `filter[field.operator]=value`
- [ ] Operators: `eq`, `contains`, `startsWith`, `endsWith`, `gt`, `gte`, `lt`,
  `lte`, `in`, `between`
- [ ] Only fields declared `Sortable()` / filterable in `QueryDefinition`

### 4e. Idempotency

- [ ] `Idempotency-Key` header for POST endpoints (client-generated UUID)
- [ ] Concurrent same-key requests return `409` with `Retry-After`
- [ ] Different payload with same key returns `422`
- [ ] **Ingestion-specific**: transport-level dedup via Redis on message ID
  (TTL 5 min) — do NOT re-implement with `Idempotency-Key`; respect the
  design decision in CLAUDE.md

### 4f. Cache-Control headers

- [ ] User data (`GET /me`, `/settings`): `private, no-cache`
- [ ] Reference data: `public, max-age=3600`
- [ ] Paginated lists: `private, no-store` (GDPR — sensitive data)
- [ ] Static resources: `public, max-age=86400, immutable`
- [ ] **NEVER** `public` for telemetry responses (may contain PII if payload
  includes user identifiers) or device credentials

### 4g. Exception handling middleware

- [ ] `app.UseGranitExceptionHandling()` registered as **first** middleware
- [ ] Custom exception mappers implement `IExceptionStatusCodeMapper`
- [ ] `IUserFriendlyException` for user-facing messages
- [ ] `ExposeInternalErrorDetails` is `false` in production (ISO 27001)
- [ ] `IHasErrorCode` for structured error codes in `ProblemDetails`

Ref: `docs_search` → "exception handling"

---

## 5. OpenAPI endpoint metadata (`--scope openapi`)

Every endpoint MUST declare all 5 elements:

- [ ] `.WithName("VerbNoun")` — PascalCase operation ID
- [ ] `.WithSummary("Imperative sentence.")` — ~100 chars, ends with period
- [ ] `.WithDescription("2-4 sentences...")` — what, context, errors
- [ ] `.Produces<T>()` — success response type (mapped from handler return type)
- [ ] `.ProducesProblem(StatusCodes.StatusXxx)` — one per error path

### Return type mapping

| Handler return | Produces | ProducesProblem |
| ---------------- | ---------- | ----------------- |
| `Ok<T>` | `.Produces<T>()` | — |
| `Created<T>` | `.Produces<T>(Status201Created)` | — |
| `Created` (no body) | `.Produces(Status201Created)` | — |
| `NoContent` | `.Produces(Status204NoContent)` | — |
| `Accepted` | `.Produces(Status202Accepted)` | — |
| `NotFound` in `Results` | — | `.ProducesProblem(Status404NotFound)` |
| `ProblemHttpResult` in `Results` | — | `.ProducesProblem(StatusXxx)` |
| `ValidationProblem` in `Results` | — | `.ProducesValidationProblem()` |

### Route groups

- [ ] `endpoints.MapGranitGroup(prefix)` — not `MapGroup()`
- [ ] Route prefix is kebab-case and plural

### Schema examples

- [ ] `ISchemaExampleProvider` implemented for request DTOs with realistic values
  (e.g. Scaleway ingestion payload)
- [ ] Internal-only endpoints marked with `[InternalApiAttribute]`

### API documentation

- [ ] Never Swashbuckle / NSwag — native `Microsoft.AspNetCore.OpenApi` only
- [ ] Scalar UI for documentation
- [ ] One OpenAPI document per API major version

Ref: `docs_search` → "api documentation"

---

## 6. Persistence (`--scope persistence`)

### 6a. Isolated DbContext

- [ ] `IoTDbContext` lives in `Granit.IoT.EntityFrameworkCore`
- [ ] `<ProjectReference>` to `Granit.Persistence`
- [ ] Constructor injects `ICurrentTenant?` and `IDataFilter?` (both optional,
  default `null`)
- [ ] `modelBuilder.ApplyGranitConventions(currentTenant, dataFilter)` called
  **last** in `OnModelCreating`
- [ ] Registration via `AddGranitDbContext<IoTDbContext>(...)` — not manual
  `AddDbContext<>()`
- [ ] Interceptors wired via `(sp, options)` overload
- [ ] `[DependsOn(typeof(GranitPersistenceModule))]` on module class
- [ ] No manual `HasQueryFilter` — `ApplyGranitConventions` handles all filters
- [ ] Named query filters via `HasQueryFilter(name, expr)` if custom filters
  needed

Ref: `docs_search` → "persistence", "query filters"

### 6b. Entity configuration

- [ ] `IMultiTenant` entities use `Guid? TenantId` (nullable — never
  non-nullable `Guid`, never `string`)
- [ ] Entity type configurations in `EntityTypeConfiguration/` folder
- [ ] Each configuration in its own file

### 6c. JSONB telemetry model (repo-specific)

Per CLAUDE.md §Key design decisions — telemetry uses JSONB, not EAV.

- [ ] Telemetry entity has `Metrics` property of type `JsonDocument` (or
  strongly-typed JSONB equivalent), mapped with `HasColumnType("jsonb")`
- [ ] GIN index on `Metrics` column declared via `.HasIndex(...).HasMethod("gin")`
- [ ] BRIN index on `recorded_at` declared via `.HasIndex(...).HasMethod("brin")`
- [ ] Monthly partitioning strategy configured (or documented as pending for a
  later phase)
- [ ] No EAV fallback tables — one row per device payload

### 6d. Interceptor pipeline awareness

Five interceptors execute in strict order:

1. `AuditedEntityInterceptor` — `CreatedAt/By`, `ModifiedAt/By`, auto-Id, TenantId
2. `VersioningInterceptor` — `VersionId`, `Version` on `IVersioned`
3. `ConcurrencyStampInterceptor` — regenerates `ConcurrencyStamp` on
   `IConcurrencyAware`
4. `DomainEventDispatcherInterceptor` — collects/dispatches domain events
5. `SoftDeleteInterceptor` — converts DELETE to UPDATE for `ISoftDeletable`

Critical bypass rules:

- [ ] `ExecuteUpdate()` bypasses ALL interceptors — must set audit fields,
  concurrency stamp, and soft-delete flag explicitly
- [ ] `ExecuteDelete()` bypasses `SoftDeleteInterceptor` — use `ExecuteUpdate()`
  with explicit `IsDeleted = true` or `DbContext.Remove()`
- [ ] For disconnected CQRS updates: explicitly set
  `db.Entry(entity).Property(e => e.ConcurrencyStamp).OriginalValue`
- [ ] Telemetry purge job (`iot-telemetry-purge`) uses `ExecuteDelete()` for
  performance — document the bypass and keep soft-delete semantics off
  telemetry entities

Ref: `docs_search` → "interceptors"

### 6e. Concurrency

- [ ] `IConcurrencyAware` entities have `ConcurrencyStamp` property
- [ ] Response DTOs include `ConcurrencyStamp`
- [ ] Request DTOs accept stamp back (implement `IConcurrencyStampRequest`)
- [ ] `DbUpdateConcurrencyException` mapped to HTTP 409 via
  `EfCoreExceptionStatusCodeMapper`
- [ ] Tests use Testcontainers (PostgreSQL) — never `UseInMemoryDatabase()` for
  concurrency tests (ignores tokens → false positive)

### 6f. Migrations

- [ ] Migration files in `Migrations/` folder
- [ ] No data seeding in migrations (use `IDataSeedContributor`)
- [ ] `DropColumn` requires Contract-phase annotation (GRMIGA001)
- [ ] `RenameColumn` not zero-downtime safe (GRMIGA002)
- [ ] `AddColumn NOT NULL` without default risks table lock (GRMIGA003)
- [ ] `AlterColumn` type change requires Contract-phase annotation (GRMIGA004)
- [ ] Column renames, type changes, splits use Expand & Contract pattern

Ref: `docs_search` → "migrations"

---

## 7. DDD (`--scope ddd`)

### 7a. Aggregate roots

- [ ] `Device` inherits `FullAuditedAggregateRoot` (per CLAUDE.md §Module
  conventions)
- [ ] All properties: `{ get; private set; }`
- [ ] Factory method: `public static Device Register(...)` — only construction path
- [ ] Private EF Core constructor: `private Device() { }` — required for
  materialization
- [ ] Behavior methods for state transitions (`RegisterHeartbeat()`,
  `Decommission()`, `ApplyThreshold()`)
- [ ] Domain events via `AddDomainEvent()` / `AddDistributedEvent()` — never
  manual `IDomainEventSource` implementation
- [ ] Explicit `IMultiTenant` interface if `TenantId` has `private set`:
  `Guid? IMultiTenant.TenantId { get => TenantId; set => TenantId = value; }`
- [ ] No public setters (enforced by architecture tests)

### 7b. Classification criteria (ADR-017)

Use `AggregateRoot` when:

- Entity has state machine (status transitions with business rules)
- Raises domain or integration events
- Encapsulates invariants

Use plain `Entity` (anemic, legitimate) when:

- Append-only / immutable after creation (telemetry rows, audit logs)
- Configuration record (thresholds-as-data)
- Cache/mirror of external system
- Lookup table (reference data)

- [ ] `Device` is an aggregate root
- [ ] Telemetry rows are plain `Entity` (append-only, JSONB payload)
- [ ] Threshold configs — classify per ADR-017 criteria; document the choice

Ref: `docs_search` → "aggregate value object strategy"

### 7c. Value objects

- [ ] Device credentials (API key, cert thumbprint, etc.) are value objects —
  inherit `SingleValueObject<T>` for single-primitive wrappers (per CLAUDE.md
  §Module conventions)
- [ ] `sealed` with `init` properties
- [ ] `Create()` factory with validation
- [ ] Implicit operators for backward compat
- [ ] EF Core converters auto-applied by `ApplyGranitConventions`
- [ ] Credentials encrypted at rest via `IStringEncryptionService`

### 7d. Entity hierarchy

Verify correct base class selection:

| Base class | When |
| ------------ | ------ |
| `Entity` | No audit fields needed — telemetry row |
| `CreationAuditedEntity` | Created tracking only |
| `AuditedEntity` | Created + Modified tracking |
| `FullAuditedEntity` | + Soft delete |
| `AggregateRoot` | Domain events, no audit |
| `CreationAuditedAggregateRoot` | Events + Created |
| `AuditedAggregateRoot` | Events + Created + Modified |
| `FullAuditedAggregateRoot` | Events + Created + Modified + Soft delete |

---

## 8. Validation (`--scope validation`)

### 8a. Validator conventions

- [ ] Every `*Request` type has a corresponding `AbstractValidator<T>`
- [ ] Validators are auto-discovered by `GranitValidationModule`
  (no manual registration)
- [ ] Route groups use `MapGranitGroup(prefix)` for auto-validation
- [ ] Opt-out via `SkipAutoValidationAttribute` only when justified
- [ ] Ingestion validation happens **before** enqueueing to Wolverine — never
  accept malformed payloads into the outbox

### 8b. Localized messages

- [ ] No hardcoded `.WithMessage("...")` strings
- [ ] Built-in validators use auto-converted error codes
  (`GranitErrorCodeLanguageManager`)
- [ ] Custom `.Must()` validators use
  `.WithErrorCodeAndMessage("Granit:IoT:Validation:XxxCode")`
- [ ] Error code present in all 18 JSON files for the satellite's localization
  resource

---

## 9. Events (`--scope events`)

### 9a. Domain events

- [ ] Implement `IDomainEvent`
- [ ] `*Event` suffix (past-tense verb + `Event`)
- [ ] Raised via `AddDomainEvent()` — synchronous, same transaction
- [ ] Handlers run after commit (`SavedChanges`)

### 9b. Integration events

- [ ] Implement `IIntegrationEvent`
- [ ] `*Eto` suffix (past-tense verb + `Eto`)
- [ ] Raised via `AddDistributedEvent()` — durable, Wolverine outbox
- [ ] Persisted atomically before commit (`SavingChanges`)
- [ ] ETO design: flat, serializable (primitives, Guids, value types only) —
  no lazy-loaded navigations or DI services
- [ ] ETO is self-contained snapshot — stable across service boundaries

### 9c. Generic lifecycle events

- [ ] `IEmitEntityLifecycleEvents` marker for local domain events only
- [ ] `IHasEntityEto<TEto>` for local + distributed events
- [ ] `ToEto()` method returns flat serializable snapshot
- [ ] Soft delete (`IsDeleted` transition) dispatches
  `EntityDeletedEvent<Device>` — not update event

### 9d. Event infrastructure

- [ ] For distributed events: `[DependsOn(typeof(GranitEventsWolverineModule))]`
- [ ] Without Wolverine: ETOs silently dropped (intentional)
- [ ] Wolverine handlers in `Granit.IoT.Wolverine` are **idempotent** (retry-safe)
- [ ] `TelemetryIngestedHandler` evaluates thresholds after persistence, not before

Ref: `docs_search` → "entity lifecycle events"

---

## 10. Metrics and diagnostics (`--scope metrics`)

### 10a. Metrics class

- [ ] `sealed class IoTMetrics` (or satellite-specific like
  `IoTIngestionMetrics`) in `Diagnostics/` folder
- [ ] Constructor injects `IMeterFactory`
- [ ] Meter name: `"Granit.IoT"` (PascalCase; satellite variants allowed)
- [ ] Metric names: `granit.iot.{entity}.{action}` (lowercase, dot-separated)
  — e.g. `granit.iot.telemetry.ingested`, `granit.iot.device.registered`,
  `granit.iot.ingestion.deduplicated`
- [ ] Tags: `snake_case`, always include `tenant_id` (coalesced to `"global"`)
- [ ] Ingestion-specific tags: `source` (scaleway | mqtt | ...),
  `device_id` (hashed if PII), `outcome` (accepted | rejected | duplicate)
- [ ] Tags passed via `TagList`
- [ ] DI: `services.TryAddSingleton<IoTMetrics>();`

### 10b. Activity source

- [ ] `internal static class IoTActivitySource` in `Diagnostics/` folder
- [ ] Registered via `GranitActivitySourceRegistry.Register(Name)` in `Add*()`
- [ ] One `ActivitySource` per satellite that emits spans

### 10c. Health checks

- [ ] Custom health checks tagged `"readiness"` and/or `"startup"` for probe
  discovery
- [ ] Defensive 10-second timeout: `.WaitAsync(10s, cancellationToken)`
- [ ] **No PII, secrets, connection strings, or stack traces** in health check
  responses (ISO 27001 / GDPR)
- [ ] Health check registration via `AddGranit*HealthCheck()` pattern
- [ ] `CachedHealthCheck` wrapper for stampede protection (default 10s TTL)
- [ ] Scaleway / MQTT broker connectivity checks exist for the relevant
  satellite (Phase-aware: skip if the provider project is a Phase 2 stub)

### 10d. Health check paths

| Probe | Path | Behavior |
| ------- | ------ | ---------- |
| Liveness | `/health/live` | Always 200, no dependency checks |
| Readiness | `/health/ready` | Checks tagged `"readiness"` |
| Startup | `/health/startup` | Checks tagged `"startup"` |

Ref: `CLAUDE.md`, `docs_search` → "diagnostics"

---

## 11. Localization (`--scope localization`)

### 11a. Culture completeness

18 cultures required (per framework): `en`, `fr`, `nl`, `de`, `es`, `it`, `pt`,
`zh`, `ja`, `pl`, `tr`, `ko`, `sv`, `cs`, `hi`, `fr-CA`, `en-GB`, `pt-BR`

- [ ] All `src/*/Localization/**/*.json` files exist for all 18 cultures
- [ ] Regional files (`fr-CA`, `en-GB`, `pt-BR`) contain only differing keys
- [ ] Base culture files contain all keys

### 11b. Localization resources

- [ ] `internal sealed class IoTEndpointsLocalizationResource` with
  `[LocalizationResourceName]` attribute (if the satellite has endpoints)
- [ ] Permission localization keys present:
  `PermissionGroup:IoT`, `Permission:IoT.{Resource}.{Action}`
- [ ] Validation error codes present for custom validators

---

## 12. Dependencies (`--scope deps`)

### 12a. Project references

- [ ] No circular references between projects
- [ ] `[DependsOn]` on module class matches `<ProjectReference>` with `*Module`
  types
- [ ] Transitive dependencies not duplicated in `[DependsOn]`
- [ ] Alphabetical order in `[DependsOn]` attributes
- [ ] Ring discipline respected (see §1a)

### 12b. Package references

- [ ] No duplicate `<PackageReference>` entries
- [ ] Version managed via `Directory.Packages.props` (central package management)
- [ ] No pinned versions in `.csproj` — use central management
- [ ] Granit framework packages pulled from GitHub Packages (per repo
  `nuget.config`), not local `ProjectReference`

### 12c. Third-party notices

- [ ] `THIRD-PARTY-NOTICES.md` updated for any new external dependency
- [ ] No GPL/LGPL/AGPL/SSPL licensed packages (flag immediately)
- [ ] Summary table and update date current

### 12d. Multi-tenancy soft dependency

- [ ] `using Granit.MultiTenancy;` — not a hard reference unless strict
  isolation is required (GDPR → **this repo requires it**, every entity
  implements `IMultiTenant` per CLAUDE.md)
- [ ] `IsAvailable` checked before using `ICurrentTenant.Id`

Ref: `docs_search` → "multi-tenancy"

---

## 13. Compliance (`--scope compliance`)

### 13a. GDPR

- [ ] `ISoftDeletable` for logical deletion (Art. 17 right to erasure) —
  `IsDeleted`, `DeletedAt`, `DeletedBy` fields on `Device` aggregate
- [ ] Telemetry rows do NOT carry personal data; if they do, they must be
  deletable on erasure request
- [ ] 3-year minimum retention before physical purge (ISO 27001) — respected
  by `iot-telemetry-purge` job
- [ ] `IProcessingRestrictable` for Art. 18 processing restriction
- [ ] `IPersonalDataProvider` implemented for data portability export where
  applicable
- [ ] No PII in logs — never log device credentials, raw payloads, owner emails
- [ ] `private, no-store` Cache-Control on telemetry endpoints
- [ ] Device credentials hard-deleted on GDPR erasure (not soft-deleted)

Ref: `docs_search` → "compliance"

### 13b. ISO 27001

- [ ] `AuditedEntity` / `AuditedAggregateRoot` for audit trail on all business
  data (Device, Threshold, etc.)
- [ ] Audit fields populated by interceptor — never manual `CreatedAt = ...`
- [ ] Encryption at rest via `IStringEncryptionService` for device credentials
  (Vault in production, AES in dev/test)
- [ ] HTTPS-only enforced on ingestion endpoints
- [ ] `ExposeInternalErrorDetails = false` in production
- [ ] `AlwaysAllow = false` in authorization
- [ ] Timeline module records device lifecycle events (register, decommission,
  heartbeat loss)

### 13c. Roslyn analyzers

All analyzer violations must be resolved (not suppressed without justification):

| Rule | Severity | What |
| ------ | ---------- | ------ |
| GRMOD001 | Error | Cross-project `internal` reference |
| GRMIGA001 | Error | `DropColumn` needs Contract-phase annotation |
| GRMIGA002 | Error | `RenameColumn` not zero-downtime safe |
| GRMIGA003 | Warning | `AddColumn NOT NULL` without default |
| GRMIGA004 | Warning | `AlterColumn` type change needs annotation |
| GRSEC001 | Warning | `DateTime.Now/UtcNow` — use `IClock` |
| GRSEC002 | Warning | `Guid.NewGuid()` — use `IGuidGenerator` |
| GRSEC003 | Error | Hardcoded secret detected |
| GRSEC004 | Warning | Direct `IResponseCookies` — use `IGranitCookieManager` |
| GREF001 | Warning | `SaveChanges()` — use `SaveChangesAsync()` |
| GRAPI001 | Warning | `Results` — use `TypedResults` |
| GRAPI002 | Warning | `BadRequest` — use `Problem()` (RFC 7807) |

- [ ] No `#pragma warning disable` for GRMOD/GRSEC rules without justification
- [ ] `BannedSymbols.txt` entries not suppressed in source
- [ ] Architecture tests (`Granit.IoT.ArchitectureTests`) cover every satellite

### 13d. Security baseline

- [ ] No hardcoded secrets (not even in comments or examples)
- [ ] No PII in logs
- [ ] No plaintext secret storage (device credentials encrypted)
- [ ] No `new HttpClient()` — use `IHttpClientFactory`
- [ ] Ingestion endpoints authenticated (shared secret, provider signature,
  or mTLS per source)
- [ ] `.gitleaks.toml` allowlist limited to test fixtures (matches existing
  repo policy)

Ref: `CLAUDE.md §Security`, `docs_search` → "security model"

---

## 14. Documentation (`--scope docs`)

### 14a. Repo-level documentation

Since the Astro docs site lives in `granit-dotnet`, this repo relies on its own
`README.md`, `docs/`, and `CLAUDE.md`.

- [ ] `README.md` describes what the IoT module family does, who it's for, and
  how to consume it
- [ ] `README.md` lists published packages and links to NuGet / GitHub Packages
- [ ] `CLAUDE.md` reflects the current project structure (compare against
  `ls src/`)
- [ ] `THIRD-PARTY-NOTICES.md` up to date with `Directory.Packages.props`
- [ ] `SECURITY.md` present with disclosure process

### 14b. Content consistency with code

- [ ] Package/namespace references in `README.md` and `docs/` use current names
  (e.g. `Granit.IoT.Ingestion`, `IoTDbContext`, `AddIoT()`)
- [ ] `using` statements in code samples match current namespaces
- [ ] DI registration examples use current method names
- [ ] Class names in code samples match the codebase
- [ ] Interface names referenced exist in the codebase
- [ ] Configuration keys (`appsettings.json` examples) match current
  `SectionName` values

### 14c. Code samples

- [ ] Code samples compile (verify with `get_public_api` MCP against the
  published framework)
- [ ] Samples follow CLAUDE.md conventions (primary constructors, collection
  expressions, `TypedResults`, etc.)
- [ ] No deprecated patterns in samples (cross-check anti-patterns list)

### 14d. Cross-references

- [ ] Internal links in `docs/` point to existing files
- [ ] Links to framework docs (granit-dotnet Astro site) are valid — query the
  Granit MCP to verify slugs
- [ ] Architecture diagrams (Mermaid) use current module / satellite names

### 14e. Framework doc sync (cross-repo)

- [ ] If this module is referenced from the granit-dotnet docs site as an
  "IoT" addon, that page still exists and uses current names — flag if
  `docs_search` returns stale or missing pages

---

## Suppressions

Do NOT flag:

- Style-only issues (formatting, whitespace, brace placement) — handled by
  `dotnet format` and `/quality`
- Missing XML docs on `internal` members
- Test file organization or test naming style
- Generated files: `*.designer.cs`, EF migrations, `packages.lock.json`
- `packages.lock.json` changes
- `BannedSymbols.txt` entries (accepted policy)
- `.gitleaks.toml` test-fixture allowlist (accepted policy)
- Placeholder projects scoped for Phase 2 (`MQTT Bridge`, operational
  hardening) or Phase 3 (`AI/MCP`, `TimescaleDB`) — check CLAUDE.md §Phased
  delivery before flagging emptiness
- Code already covered by `Granit.IoT.ArchitectureTests` (verify coverage
  exists, do not re-check manually)
- SonarQube issues (handled by `/quality`)
