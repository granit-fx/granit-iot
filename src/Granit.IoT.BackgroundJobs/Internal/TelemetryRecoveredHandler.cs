using Granit.IoT.Events;

namespace Granit.IoT.BackgroundJobs.Internal;

/// <summary>
/// Wolverine handler that clears the offline-tracker entry for a device
/// when fresh telemetry arrives. The device becomes eligible for the next
/// offline alert (after the heartbeat threshold lapses again).
/// </summary>
public static class TelemetryRecoveredHandler
{
    public static Task HandleAsync(
        TelemetryIngestedEto eto,
        DeviceOfflineTrackerCache tracker,
        CancellationToken _)
    {
        ArgumentNullException.ThrowIfNull(eto);
        if (eto.DeviceId is { } deviceId)
        {
            tracker.Forget(deviceId);
        }
        return Task.CompletedTask;
    }
}
