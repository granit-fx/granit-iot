using Granit.IoT.BackgroundJobs.Services;

namespace Granit.IoT.BackgroundJobs.Jobs;

public class TelemetryPartitionMaintenanceHandler
{
    public static Task HandleAsync(
        TelemetryPartitionMaintenanceJob _,
        TelemetryPartitionMaintenanceService service,
        CancellationToken cancellationToken) =>
        service.ExecuteAsync(cancellationToken);
}
