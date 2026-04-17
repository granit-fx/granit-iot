using Granit.IoT.BackgroundJobs.Services;

namespace Granit.IoT.BackgroundJobs.Jobs;

public static class DeviceHeartbeatTimeoutHandler
{
    public static Task HandleAsync(
        DeviceHeartbeatTimeoutJob _,
        DeviceHeartbeatTimeoutService service,
        CancellationToken cancellationToken) =>
        service.ExecuteAsync(cancellationToken);
}
