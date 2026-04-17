using ArchUnitNET.Domain;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Validates conventions for the <c>Granit.IoT.Mcp</c> tool surface: MCP tool
/// classes must be static (abstract+sealed in IL), decorated with
/// <c>[McpServerToolType]</c>, and must not expose tenant identifiers on
/// response DTOs. These rules protect the AI layer from accidentally surfacing
/// cross-tenant data or unbounded context.
/// </summary>
public sealed class McpConventionTests
{
    private const string McpNamespacePrefix = "Granit.IoT.Mcp";
    private const string ToolsNamespacePrefix = "Granit.IoT.Mcp.Tools";
    private const string ResponsesNamespacePrefix = "Granit.IoT.Mcp.Responses";
    private const string McpServerToolTypeAttribute = "ModelContextProtocol.Server.McpServerToolTypeAttribute";
    private const string McpTenantScopeAttribute = "Granit.Mcp.McpTenantScopeAttribute";

    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void Tool_classes_should_be_public_static_and_decorated_with_McpServerToolType()
    {
        IReadOnlyList<Class> tools = Architecture.Classes
            .Where(c => c.FullName.StartsWith(ToolsNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("Tools", StringComparison.Ordinal))
            .ToList();

        tools.ShouldNotBeEmpty("Expected at least one *Tools class under Granit.IoT.Mcp.Tools.");

        IEnumerable<Class> notStatic = tools
            .Where(c => c.Visibility != Visibility.Public || c.IsAbstract != true || c.IsSealed != true);

        notStatic.ShouldBeEmpty(
            "MCP tool classes must be public static. " +
            $"Violators: {string.Join(", ", notStatic.Select(c => c.FullName))}");

        IEnumerable<Class> missingAttribute = tools
            .Where(c => !HasAttribute(c, McpServerToolTypeAttribute));

        missingAttribute.ShouldBeEmpty(
            "MCP tool classes must be decorated with [McpServerToolType]. " +
            $"Violators: {string.Join(", ", missingAttribute.Select(c => c.FullName))}");
    }

    [Fact]
    public void Tool_classes_should_require_a_tenant_scope()
    {
        IReadOnlyList<Class> tools = Architecture.Classes
            .Where(c => c.FullName.StartsWith(ToolsNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("Tools", StringComparison.Ordinal))
            .ToList();

        IEnumerable<Class> missingTenantScope = tools
            .Where(c => !HasAttribute(c, McpTenantScopeAttribute));

        missingTenantScope.ShouldBeEmpty(
            "MCP tool classes must be decorated with [McpTenantScope(RequireTenant = true)] " +
            "so TenantAwareVisibilityFilter can hide them when no tenant context is present. " +
            $"Violators: {string.Join(", ", missingTenantScope.Select(c => c.FullName))}");
    }

    [Fact]
    public void Response_records_must_not_expose_TenantId()
    {
        IReadOnlyList<Class> responses = Architecture.Classes
            .Where(c => c.FullName.StartsWith(ResponsesNamespacePrefix, StringComparison.Ordinal))
            .ToList();

        responses.ShouldNotBeEmpty("Expected at least one response record under Granit.IoT.Mcp.Responses.");

        IEnumerable<(Class Type, string Property)> leaks =
            from c in responses
            from field in c.Members.OfType<FieldMember>()
            where field.Name.Contains("TenantId", StringComparison.Ordinal)
            select (c, field.Name);

        leaks.ShouldBeEmpty(
            "MCP response records must not expose TenantId — AI assistants must never " +
            "surface cross-tenant identifiers in their answers. " +
            $"Violators: {string.Join(", ", leaks.Select(l => $"{l.Type.FullName}.{l.Property}"))}");
    }

    [Fact]
    public void Module_should_live_at_the_root_of_the_Mcp_namespace()
    {
        var modules = Architecture.Classes
            .Where(c => c.FullName.StartsWith(McpNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("Module", StringComparison.Ordinal))
            .ToList();

        modules.Count.ShouldBe(1,
            "Granit.IoT.Mcp must expose exactly one GranitModule.");
        modules[0].FullName.ShouldBe("Granit.IoT.Mcp.GranitIoTMcpModule");
    }

    private static bool HasAttribute(Class type, string attributeFullName) =>
        type.Attributes.Any(a => a.FullName == attributeFullName);
}
