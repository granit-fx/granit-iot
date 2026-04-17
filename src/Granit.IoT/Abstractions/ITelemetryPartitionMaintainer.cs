namespace Granit.IoT.Abstractions;

/// <summary>
/// Provider-specific maintenance for the partitioned telemetry table.
/// Implemented in <c>Granit.IoT.EntityFrameworkCore.Postgres</c>; consumed by
/// <c>TelemetryPartitionMaintenanceJob</c>.
/// </summary>
/// <remarks>
/// If the parent table is not partitioned (consumer never called
/// <c>EnableTelemetryPartitioning()</c>), <see cref="IsParentPartitionedAsync"/>
/// returns <c>false</c> and the job exits gracefully — partitioning remains
/// strictly opt-in. A no-op implementation may be registered for non-Postgres
/// providers.
/// </remarks>
public interface ITelemetryPartitionMaintainer
{
    /// <summary>Returns whether the telemetry table is RANGE-partitioned.</summary>
    Task<bool> IsParentPartitionedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the monthly partition for the given (year, month) attached to the
    /// parent table, with partition-local BRIN(RecordedAt) and GIN(Metrics)
    /// indexes. Idempotent.
    /// </summary>
    Task CreatePartitionAsync(int year, int month, CancellationToken cancellationToken = default);
}
