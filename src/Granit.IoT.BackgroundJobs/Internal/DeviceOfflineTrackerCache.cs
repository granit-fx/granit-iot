using Microsoft.Extensions.Caching.Memory;

namespace Granit.IoT.BackgroundJobs.Internal;

/// <summary>
/// Process-local debouncing cache: keeps a device flagged as already-alerted
/// for a caller-supplied TTL. Prevents alert spam on flaky links (LoRa /
/// NB-IoT) where a device flaps in and out of reach repeatedly. The cache is
/// process-local because the heartbeat job is single-leader scheduled by
/// <c>Granit.BackgroundJobs</c>; no cross-node coordination is required.
/// </summary>
public sealed class DeviceOfflineTrackerCache(IMemoryCache cache)
{
    /// <summary>
    /// Returns <c>true</c> if the device was newly added to the tracker (the
    /// caller should publish the alert), <c>false</c> if it was already tracked
    /// (suppress to avoid spam).
    /// </summary>
    public bool TryAdd(Guid deviceId, TimeSpan ttl)
    {
        string key = Key(deviceId);
        if (cache.TryGetValue(key, out _))
        {
            return false;
        }
        cache.Set(key, true, ttl);
        return true;
    }

    /// <summary>
    /// Removes the tracker entry so the device is eligible for the next alert.
    /// Called when fresh telemetry arrives (the device is back online).
    /// </summary>
    public void Forget(Guid deviceId) => cache.Remove(Key(deviceId));

    private static string Key(Guid deviceId) => $"iot:offline-tracker:{deviceId:N}";
}
