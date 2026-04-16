using Granit.IoT.Domain;

namespace Granit.IoT.Abstractions;

/// <summary>Persists telemetry data (command side of CQRS). Append-only — no update or delete.</summary>
public interface ITelemetryWriter
{
    /// <summary>Appends a single telemetry point.</summary>
    Task AppendAsync(TelemetryPoint point, CancellationToken cancellationToken = default);

    /// <summary>Appends a batch of telemetry points in a single database round-trip.</summary>
    Task AppendBatchAsync(IReadOnlyList<TelemetryPoint> points, CancellationToken cancellationToken = default);
}
