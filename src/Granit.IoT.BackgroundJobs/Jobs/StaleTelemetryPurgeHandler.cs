using Granit.IoT.BackgroundJobs.Services;

namespace Granit.IoT.BackgroundJobs.Jobs;

/// <summary>Handler wired to <see cref="StaleTelemetryPurgeJob"/> — delegates to <see cref="StaleTelemetryPurgeService.ExecuteAsync"/>.</summary>
public static class StaleTelemetryPurgeHandler
{
    /// <summary>Invoked by the recurring-job scheduler for every <see cref="StaleTelemetryPurgeJob"/> tick.</summary>
    public static Task HandleAsync(
        StaleTelemetryPurgeJob _,
        StaleTelemetryPurgeService service,
        CancellationToken cancellationToken) =>
        service.ExecuteAsync(cancellationToken);
}
