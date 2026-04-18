using Granit.IoT.BackgroundJobs.Services;

namespace Granit.IoT.BackgroundJobs.Jobs;

/// <summary>Handler wired to <see cref="TelemetryPartitionMaintenanceJob"/> — delegates to <see cref="TelemetryPartitionMaintenanceService.ExecuteAsync"/>.</summary>
public static class TelemetryPartitionMaintenanceHandler
{
    /// <summary>Invoked by the recurring-job scheduler for every <see cref="TelemetryPartitionMaintenanceJob"/> tick.</summary>
    public static Task HandleAsync(
        TelemetryPartitionMaintenanceJob _,
        TelemetryPartitionMaintenanceService service,
        CancellationToken cancellationToken) =>
        service.ExecuteAsync(cancellationToken);
}
