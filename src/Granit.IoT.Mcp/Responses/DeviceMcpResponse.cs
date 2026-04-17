namespace Granit.IoT.Mcp.Responses;

/// <summary>
/// Device projection returned by MCP tools. Excludes tenant ID, credentials, and
/// suspension details so an AI assistant never surfaces cross-tenant identifiers
/// or sensitive fields in its responses.
/// </summary>
public sealed record DeviceMcpResponse(
    Guid Id,
    string SerialNumber,
    string Status,
    string Model,
    string Firmware,
    string? Label,
    DateTimeOffset? LastSeenAt);
