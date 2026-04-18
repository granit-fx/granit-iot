using System.ComponentModel;
using System.Text;
using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.Mcp.Responses;
using Granit.Mcp;
using ModelContextProtocol.Server;

namespace Granit.IoT.Mcp.Tools;

/// <summary>
/// MCP tools exposing the IoT device fleet to AI assistants. All queries go through
/// <see cref="IDeviceReader"/> which is tenant-scoped by EF Core query filters —
/// cross-tenant access is impossible regardless of what an AI agent sends.
/// </summary>
[McpServerToolType]
[McpExposed]
[McpTenantScope(RequireTenant = true)]
public static class DeviceMcpTools
{
    /// <summary>Lists IoT devices for the current tenant with an optional status filter and pagination.</summary>
    [McpServerTool(Name = "iot_list_devices")]
    [Description(
        "Lists IoT devices for the current tenant. Optional filter by status: " +
        "'Provisioning', 'Active', 'Suspended', 'Decommissioned'. " +
        "Use this to answer fleet-wide questions like 'how many devices are offline?' " +
        "or 'which devices are suspended?'.")]
    public static async Task<IReadOnlyList<DeviceMcpResponse>> ListAsync(
        IDeviceReader reader,
        [Description("Optional device status filter. Leave empty to list all statuses.")]
        string? statusFilter = null,
        [Description("Page number (1-based). Default 1.")]
        int page = 1,
        [Description("Page size. Default 20, capped at 100.")]
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        DeviceStatus? parsedStatus = ParseStatus(statusFilter);
        int cappedPage = page < 1 ? 1 : page;
        int cappedPageSize = Math.Clamp(pageSize, 1, 100);

        IReadOnlyList<Device> devices = await reader
            .ListAsync(parsedStatus, cappedPage, cappedPageSize, cancellationToken)
            .ConfigureAwait(false);

        return devices.Select(ToResponse).ToArray();
    }

    /// <summary>Returns a single IoT device by id, or <c>null</c> if not found in the current tenant.</summary>
    [McpServerTool(Name = "iot_get_device")]
    [Description(
        "Returns a single IoT device by its ID, or null if not found or not in the " +
        "current tenant. Use this to answer targeted questions like 'is device X active?' " +
        "or 'when did device Y last check in?'.")]
    public static async Task<DeviceMcpResponse?> GetAsync(
        IDeviceReader reader,
        [Description("Device identifier (GUID).")]
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        Device? device = await reader.FindAsync(deviceId, cancellationToken).ConfigureAwait(false);
        return device is null ? null : ToResponse(device);
    }

    private static DeviceStatus? ParseStatus(string? statusFilter)
    {
        if (string.IsNullOrWhiteSpace(statusFilter))
        {
            return null;
        }

        return Enum.TryParse<DeviceStatus>(statusFilter, ignoreCase: true, out DeviceStatus parsed)
            ? parsed
            : null;
    }

    /// <summary>Maximum label length surfaced to AI agents.</summary>
    internal const int MaxLabelLength = 128;

    private static DeviceMcpResponse ToResponse(Device device) => new(
        Id: device.Id,
        SerialNumber: device.SerialNumber.Value,
        Status: device.Status.ToString(),
        Model: device.Model.Value,
        Firmware: device.Firmware.Value,
        Label: SanitizeLabel(device.Label),
        LastSeenAt: device.LastHeartbeatAt);

    /// <summary>
    /// Device labels are tenant-supplied free text. Returning them raw to an
    /// AI agent is a prompt-injection vector — a crafted label can carry
    /// instructions the LLM will follow. Strip control and unprintable code
    /// points, collapse whitespace, and cap the length.
    /// </summary>
    internal static string? SanitizeLabel(string? label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return label;
        }

        StringBuilder sb = new(capacity: Math.Min(label.Length, MaxLabelLength));
        bool prevWasSpace = false;
        foreach (char c in label)
        {
            if (sb.Length >= MaxLabelLength)
            {
                break;
            }

            if (char.IsControl(c) || c == '\u200B' || c == '\uFEFF')
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (prevWasSpace)
                {
                    continue;
                }

                sb.Append(' ');
                prevWasSpace = true;
                continue;
            }

            sb.Append(c);
            prevWasSpace = false;
        }

        return sb.ToString().Trim();
    }
}
