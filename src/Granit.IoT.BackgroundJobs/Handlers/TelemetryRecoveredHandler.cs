using Granit.IoT.BackgroundJobs.Internal;
using Granit.IoT.Events;

namespace Granit.IoT.BackgroundJobs.Handlers;

/// <summary>
/// Wolverine handler that clears the offline-tracker entry for a device
/// when fresh telemetry arrives. The device becomes eligible for the next
/// offline alert (after the heartbeat threshold lapses again).
/// </summary>
public static class TelemetryRecoveredHandler
{
    /// <summary>
    /// Handles <see cref="TelemetryIngestedEto"/> — forgets the device from
    /// the offline tracker so the next missed heartbeat can raise a fresh alert.
    /// </summary>
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
