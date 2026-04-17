# Granit.IoT.Mcp

Exposes Granit.IoT device and telemetry readers as MCP (Model Context Protocol)
tools so AI assistants (Claude, Copilot, any MCP-aware client) can query the IoT
fleet in natural language.

Without this package, an AI agent connected to a Granit MCP server cannot answer
"which devices are currently offline?" or "what was the temperature of cold
chain number 4 over the last two hours?" — it has no tools bound to the IoT
domain. This package adds four read-only tools that wrap `IDeviceReader` and `ITelemetryReader`
with zero business logic: queries go through the same CQRS readers the rest of
the application uses, so multi-tenancy and permissions apply unchanged.

## What it ships

- `DeviceMcpTools` — static tool class
  - `iot_list_devices(statusFilter?, page, pageSize)` — lists devices for the
    current tenant, optional `DeviceStatus` filter
  - `iot_get_device(deviceId)` — returns a single device or `null`
- `TelemetryMcpTools` — static tool class
  - `iot_query_telemetry(deviceId, metricName, from, to, maxPoints = 100)` —
    time-windowed query, metric filtered, `maxPoints` capped at **1000**
  - `iot_get_latest_readings(deviceId)` — expands the most recent `TelemetryPoint`
    into one reading per metric
- `DeviceMcpResponse` / `TelemetryReadingMcpResponse` — DTOs stripped of
  `TenantId`, credentials, raw payload IDs, and source markers

## Tenant isolation — non-negotiable

Both tool classes are decorated with `[McpTenantScope(RequireTenant = true)]`.
The framework's `TenantAwareVisibilityFilter` (in `Granit.Mcp.Server`) hides
these tools from the MCP manifest entirely when `ICurrentTenant.IsAvailable`
is `false`. Combined with EF Core query filters on `IDeviceReader` /
`ITelemetryReader`, cross-tenant access is impossible at two layers.

> [!IMPORTANT]
> The `maxPoints` parameter on `iot_query_telemetry` is **silently capped at 1000**.
> AI context windows cost tokens; a rogue or naive prompt asking for "all history"
> would otherwise return tens of thousands of points. The cap is documented in the
> tool's `[Description]` so the AI knows to paginate or aggregate instead.

## Setup

Bundled in `Granit.Bundle.IoT`:

```csharp
builder.Services.AddGranit(builder.Configuration).AddIoT();
```

Or standalone:

```csharp
builder.Services
    .AddGranit(builder.Configuration)
    .AddModule<GranitIoTMcpModule>();
```

Tools are auto-discovered by `GranitMcpModule` via assembly scanning — there is
no manual `WithTools<T>()` call to make. Adding the module is sufficient.

## Example AI conversation

> **User (to Claude)**: "How many of my cold-chain sensors are currently suspended?"
>
> **Claude**: *(calls `iot_list_devices` with `statusFilter = "Suspended"`)*
> You have **three suspended devices**: `CC-042` (suspended 2 hours ago,
> reason: "battery replacement"), `CC-107` (suspended yesterday), and `CC-119`
> (suspended last week).

## Anti-patterns to avoid

> [!WARNING]
> **Don't add write tools here.** MCP tools in this package are read-only on
> purpose — an AI agent must not provision, suspend, or decommission devices
> through natural-language prompts. Writes belong behind explicit HTTP endpoints
> with permission checks, audit trail, and human approval.

> [!WARNING]
> **Don't leak `TenantId` through custom response DTOs.** The architecture test
> `McpConventionTests.Response_records_must_not_expose_TenantId` fails the build
> if a field named `*TenantId` appears on any record under
> `Granit.IoT.Mcp.Responses`. Tenant identifiers are infrastructure, not
> conversation context.

## See also

- [architecture](../../docs/architecture.md) — ring structure (this is Ring 3)
- [Granit.IoT](../Granit.IoT/README.md) — domain and CQRS reader abstractions
- [Granit.Mcp.Server](https://github.com/granit-fx/granit-dotnet) — the server
  that hosts these tools over Streamable HTTP
