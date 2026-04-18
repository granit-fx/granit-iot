using System.ComponentModel;
using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.Mcp.Responses;
using Granit.Mcp;
using ModelContextProtocol.Server;

namespace Granit.IoT.Mcp.Tools;

/// <summary>
/// MCP tools exposing IoT telemetry history to AI assistants. Bounded by a hard cap
/// (<see cref="MaxPointsLimit"/>) on every query to protect the AI context window
/// from unbounded data injection — critical for cost and answer quality.
/// </summary>
[McpServerToolType]
[McpExposed]
[McpTenantScope(RequireTenant = true)]
public static class TelemetryMcpTools
{
    /// <summary>Upper bound enforced on <c>maxPoints</c> to cap AI context growth.</summary>
    public const int MaxPointsLimit = 1000;

    /// <summary>Default <c>maxPoints</c> when the caller omits the parameter.</summary>
    public const int DefaultMaxPoints = 100;

    /// <summary>
    /// Returns telemetry readings for the given device, metric and time window,
    /// capped at <see cref="MaxPointsLimit"/>.
    /// </summary>
    [McpServerTool(Name = "iot_query_telemetry")]
    [Description(
        "Returns telemetry readings for a given device, metric, and time window, " +
        "ordered by RecordedAt descending. The maxPoints parameter is silently " +
        "capped at 1000 to protect the AI context window. Use this for anomaly " +
        "investigation — e.g. 'what was the temperature of cold chain #4 over the " +
        "last 2 hours?'.")]
    public static async Task<IReadOnlyList<TelemetryReadingMcpResponse>> QueryAsync(
        ITelemetryReader reader,
        [Description("Device identifier (GUID).")]
        Guid deviceId,
        [Description("Metric name to filter (e.g. 'temperature', 'humidity'). Case-sensitive.")]
        string metricName,
        [Description("Start of the time window (inclusive, UTC ISO-8601).")]
        DateTimeOffset from,
        [Description("End of the time window (inclusive, UTC ISO-8601).")]
        DateTimeOffset to,
        [Description("Maximum number of points to return. Default 100, capped at 1000.")]
        int maxPoints = DefaultMaxPoints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName);

        int cappedMaxPoints = Math.Clamp(maxPoints, 1, MaxPointsLimit);

        IReadOnlyList<TelemetryPoint> points = await reader
            .QueryAsync(deviceId, from, to, cappedMaxPoints, cancellationToken)
            .ConfigureAwait(false);

        return points
            .Where(p => p.Metrics.ContainsKey(metricName))
            .Select(p => new TelemetryReadingMcpResponse(
                MetricName: metricName,
                Value: p.Metrics[metricName],
                RecordedAt: p.RecordedAt))
            .ToArray();
    }

    /// <summary>Returns the most recent telemetry point for a device, expanded into one reading per metric.</summary>
    [McpServerTool(Name = "iot_get_latest_readings")]
    [Description(
        "Returns the most recent telemetry point for a device, expanded into one " +
        "reading per metric. Ideal for 'current state' questions like 'what is the " +
        "current temperature of device X?'. Returns an empty list if the device has " +
        "no telemetry yet.")]
    public static async Task<IReadOnlyList<TelemetryReadingMcpResponse>> GetLatestReadingsAsync(
        ITelemetryReader reader,
        [Description("Device identifier (GUID).")]
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        TelemetryPoint? latest = await reader
            .GetLatestAsync(deviceId, cancellationToken)
            .ConfigureAwait(false);

        if (latest is null)
        {
            return Array.Empty<TelemetryReadingMcpResponse>();
        }

        return latest.Metrics
            .Select(kvp => new TelemetryReadingMcpResponse(
                MetricName: kvp.Key,
                Value: kvp.Value,
                RecordedAt: latest.RecordedAt))
            .ToArray();
    }
}
