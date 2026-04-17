using Granit.BackgroundJobs;

namespace Granit.IoT.BackgroundJobs.Jobs;

/// <summary>
/// Recurring job that creates the next two monthly partitions for
/// <c>iot_telemetry_points</c>. Runs every Sunday at 01:00 UTC.
/// Gracefully no-ops when the parent table is not partitioned.
/// </summary>
[RecurringJob("0 1 * * 0", "iot-partition-maintenance")]
public sealed record TelemetryPartitionMaintenanceJob : IBackgroundJob;
