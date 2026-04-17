using Granit.IoT.BackgroundJobs.Services;

namespace Granit.IoT.BackgroundJobs.Jobs;

public class StaleTelemetryPurgeHandler
{
    public static Task HandleAsync(
        StaleTelemetryPurgeJob _,
        StaleTelemetryPurgeService service,
        CancellationToken cancellationToken) =>
        service.ExecuteAsync(cancellationToken);
}
