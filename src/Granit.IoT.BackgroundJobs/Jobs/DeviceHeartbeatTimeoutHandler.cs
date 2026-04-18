using Granit.IoT.BackgroundJobs.Services;

namespace Granit.IoT.BackgroundJobs.Jobs;

/// <summary>Handler wired to <see cref="DeviceHeartbeatTimeoutJob"/> — delegates to <see cref="DeviceHeartbeatTimeoutService.ExecuteAsync"/>.</summary>
public static class DeviceHeartbeatTimeoutHandler
{
    /// <summary>Invoked by the recurring-job scheduler for every <see cref="DeviceHeartbeatTimeoutJob"/> tick.</summary>
    public static Task HandleAsync(
        DeviceHeartbeatTimeoutJob _,
        DeviceHeartbeatTimeoutService service,
        CancellationToken cancellationToken) =>
        service.ExecuteAsync(cancellationToken);
}
