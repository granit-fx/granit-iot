using Granit.Events;
using Granit.IoT.Abstractions;
using Granit.IoT.BackgroundJobs.Internal;
using Granit.IoT.Diagnostics;
using Granit.IoT.Domain;
using Granit.IoT.Events;
using Granit.IoT.Notifications;
using Granit.MultiTenancy;
using Granit.Settings.Services;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.BackgroundJobs.Services;

/// <summary>
/// Bucketed offline-detection: discovers tenants from the device table, resolves
/// each tenant's <see cref="IoTSettingNames.HeartbeatTimeoutMinutes"/>, groups
/// tenants by their effective value, and issues one
/// <see cref="IDeviceReader.FindStaleAsync(IReadOnlyCollection{Guid?}, DateTimeOffset, int, CancellationToken)"/>
/// call per bucket. Eliminates the N+1 SQL pattern. A 4-min cancellation
/// deadline on the 5-min cron prevents overlap between consecutive runs.
/// </summary>
/// <remarks>
/// The job is advisory — it does <b>not</b> mutate device status. It publishes
/// <see cref="DeviceOfflineDetectedEto"/> for downstream notification handlers,
/// debouncing repeated alerts via <see cref="DeviceOfflineTrackerCache"/> so a
/// flapping device won't blow up SMTP quotas.
/// </remarks>
public sealed partial class DeviceHeartbeatTimeoutService(
    IDeviceReader deviceReader,
    ISettingProvider settings,
    ICurrentTenant currentTenant,
    IDistributedEventBus eventBus,
    DeviceOfflineTrackerCache tracker,
    IoTMetrics metrics,
    TimeProvider clock,
    ILogger<DeviceHeartbeatTimeoutService> logger)
{
    internal const int DefaultTimeoutMinutes = 15;
    internal const int DefaultCacheMinutes = 60;
    internal const int BatchSize = 5000;
    private static readonly TimeSpan JobDeadline = TimeSpan.FromMinutes(4);

    public async Task ExecuteAsync(CancellationToken jobCt)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(jobCt);
        cts.CancelAfter(JobDeadline);
        CancellationToken ct = cts.Token;

        IReadOnlyList<Guid?> tenantIds = await deviceReader
            .GetDistinctTenantIdsAsync(ct)
            .ConfigureAwait(false);

        if (tenantIds.Count == 0)
        {
            return;
        }

        List<(Guid? TenantId, int Minutes)> resolved = await ResolvePerTenantTimeoutAsync(tenantIds, ct).ConfigureAwait(false);
        if (resolved.Count == 0)
        {
            return;
        }

        TimeSpan trackerTtl = await ResolveTrackerTtlAsync(ct).ConfigureAwait(false);
        DateTimeOffset now = clock.GetUtcNow();
        foreach (IGrouping<int, Guid?> bucket in resolved.GroupBy(r => r.Minutes, r => r.TenantId))
        {
            ct.ThrowIfCancellationRequested();
            DateTimeOffset cutoff = now.AddMinutes(-bucket.Key);
            Guid?[] bucketTenants = [.. bucket];

            IReadOnlyList<Device> stale = await deviceReader
                .FindStaleAsync(bucketTenants, cutoff, BatchSize, ct)
                .ConfigureAwait(false);

            int published = 0;
            foreach (Device device in stale)
            {
                if (!tracker.TryAdd(device.Id, trackerTtl))
                {
                    continue;
                }

                metrics.RecordDeviceOfflineDetected(device.TenantId?.ToString());
                await eventBus
                    .PublishAsync(new DeviceOfflineDetectedEto(
                        device.Id,
                        device.SerialNumber,
                        device.LastHeartbeatAt,
                        device.TenantId), ct)
                    .ConfigureAwait(false);
                published++;
            }

            Log.HeartbeatBucketProcessed(logger, bucket.Key, bucketTenants.Length, stale.Count, published);
        }
    }

    private async Task<TimeSpan> ResolveTrackerTtlAsync(CancellationToken ct)
    {
        // Read the cache TTL once per run. The setting is conceptually per-tenant,
        // but the dedup window only needs to be long enough to suppress a flap.
        // Resolving against the host-context value (or the first-seen tenant scope)
        // is plenty accurate and saves a per-device async hop on the alerting hot path.
        string? raw = await settings
            .GetOrNullAsync(IoTSettingNames.HeartbeatOfflineNotificationCacheMinutes, ct)
            .ConfigureAwait(false);
        int minutes = int.TryParse(raw, out int parsed) ? parsed : DefaultCacheMinutes;
        return TimeSpan.FromMinutes(minutes);
    }

    private async Task<List<(Guid? TenantId, int Minutes)>> ResolvePerTenantTimeoutAsync(
        IReadOnlyList<Guid?> tenantIds,
        CancellationToken ct)
    {
        List<(Guid? TenantId, int Minutes)> resolved = new(tenantIds.Count);
        foreach (Guid? tenantId in tenantIds)
        {
            ct.ThrowIfCancellationRequested();
            using IDisposable _ = currentTenant.Change(tenantId);
            string? raw = await settings
                .GetOrNullAsync(IoTSettingNames.HeartbeatTimeoutMinutes, ct)
                .ConfigureAwait(false);
            int minutes = int.TryParse(raw, out int parsed) ? parsed : DefaultTimeoutMinutes;
            // Tenant disabled the feature explicitly — drop from the work set.
            if (minutes > 0)
            {
                resolved.Add((tenantId, minutes));
            }
        }
        return resolved;
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information,
            Message = "Heartbeat timeout bucket: timeout={Minutes}min, tenants={TenantCount}, stale={Stale}, published={Published}.")]
        public static partial void HeartbeatBucketProcessed(ILogger logger, int minutes, int tenantCount, int stale, int published);
    }
}
