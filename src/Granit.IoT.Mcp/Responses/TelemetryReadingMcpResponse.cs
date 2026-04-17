namespace Granit.IoT.Mcp.Responses;

/// <summary>
/// One metric reading projected from a <c>TelemetryPoint</c>. Excludes tenant ID,
/// message ID, and ingestion source so AI responses stay focused on the observed
/// value and its timestamp.
/// </summary>
public sealed record TelemetryReadingMcpResponse(
    string MetricName,
    double Value,
    DateTimeOffset RecordedAt);
