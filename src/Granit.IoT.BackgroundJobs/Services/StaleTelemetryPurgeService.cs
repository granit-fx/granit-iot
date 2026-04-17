using Granit.IoT.Abstractions;
using Granit.IoT.Diagnostics;
using Granit.IoT.Notifications;
using Granit.MultiTenancy;
using Granit.Settings.Services;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.BackgroundJobs.Services;

/// <summary>
/// Bucketed retention purge: discovers tenants from the device table, resolves
/// each tenant's <see cref="IoTSettingNames.TelemetryRetentionDays"/>, groups
/// tenants by their effective value, and issues one bulk
/// <c>ExecuteDeleteAsync</c> per bucket. Eliminates the N+1 SQL pattern and
/// caps run time via a hard cancellation deadline so a slow run cannot overlap
/// the next 03:00 UTC tick.
/// </summary>
public sealed partial class StaleTelemetryPurgeService(
    IDeviceReader deviceReader,
    ITelemetryPurger purger,
    ISettingProvider settings,
    ICurrentTenant currentTenant,
    IoTMetrics metrics,
    TimeProvider clock,
    ILogger<StaleTelemetryPurgeService> logger)
{
    internal const int DefaultRetentionDays = 365;
    private static readonly TimeSpan JobDeadline = TimeSpan.FromMinutes(30);

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

        List<(Guid? TenantId, int Days)> resolved = await ResolvePerTenantRetentionAsync(tenantIds, ct).ConfigureAwait(false);

        DateTimeOffset now = clock.GetUtcNow();
        foreach (IGrouping<int, Guid?> bucket in resolved.GroupBy(r => r.Days, r => r.TenantId))
        {
            ct.ThrowIfCancellationRequested();
            DateTimeOffset cutoff = now.AddDays(-bucket.Key);
            Guid?[] bucketTenants = [.. bucket];

            long deleted = await purger
                .PurgeOlderThanAsync(bucketTenants, cutoff, ct)
                .ConfigureAwait(false);

            // Distribute the bulk count across the bucket's tenants for an
            // evenly-attributed metric. An exact per-tenant breakdown would
            // require a follow-up GROUP BY query — not worth the round-trip
            // for an observability counter.
            long perTenant = deleted / bucketTenants.Length;
            foreach (Guid? tenantId in bucketTenants)
            {
                metrics.RecordTelemetryPurged(tenantId?.ToString(), perTenant);
            }

            Log.PurgedBucket(logger, bucket.Key, bucketTenants.Length, deleted);
        }
    }

    private async Task<List<(Guid? TenantId, int Days)>> ResolvePerTenantRetentionAsync(
        IReadOnlyList<Guid?> tenantIds,
        CancellationToken ct)
    {
        List<(Guid? TenantId, int Days)> resolved = new(tenantIds.Count);
        foreach (Guid? tenantId in tenantIds)
        {
            ct.ThrowIfCancellationRequested();
            using IDisposable _ = currentTenant.Change(tenantId);
            string? raw = await settings
                .GetOrNullAsync(IoTSettingNames.TelemetryRetentionDays, ct)
                .ConfigureAwait(false);
            int days = int.TryParse(raw, out int parsed) ? parsed : DefaultRetentionDays;
            resolved.Add((tenantId, days));
        }
        return resolved;
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information,
            Message = "Purged {Deleted} telemetry rows for {TenantCount} tenant(s) with retention = {Days} days.")]
        public static partial void PurgedBucket(ILogger logger, int days, int tenantCount, long deleted);
    }
}
