using Granit.Mcp.Server;
using Granit.Modularity;

namespace Granit.IoT.Mcp;

/// <summary>
/// Exposes Granit.IoT readers (<c>IDeviceReader</c>, <c>ITelemetryReader</c>) as MCP
/// tools so AI assistants can query the device fleet and telemetry history in natural
/// language. Tool classes are auto-discovered via <c>[McpServerToolType]</c> by
/// <c>GranitMcpModule</c>; <c>[McpTenantScope(RequireTenant = true)]</c> on each
/// tool class hides them from the MCP manifest when no tenant context is present.
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitMcpServerModule))]
public sealed class GranitIoTMcpModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
    }
}
