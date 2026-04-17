using System.Collections.Concurrent;

namespace Granit.IoT.Aws.Jobs.Internal;

/// <summary>
/// Default per-host tracking store. Sufficient when a single host owns the
/// entire fleet; production multi-instance deployments should override with
/// a Redis-backed implementation so a job dispatched on host A is observed
/// by the poller on host B.
/// </summary>
internal sealed class InMemoryJobTrackingStore(TimeProvider timeProvider) : IJobTrackingStore
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ConcurrentDictionary<Guid, JobTrackingEntry> _entries = new();

    public Task SetAsync(Guid correlationId, JobTrackingEntry entry, TimeSpan ttl, CancellationToken cancellationToken)
    {
        JobTrackingEntry withTtl = entry with { ExpiresAt = _timeProvider.GetUtcNow().Add(ttl) };
        _entries[correlationId] = withTtl;
        return Task.CompletedTask;
    }

    public Task<JobTrackingEntry?> GetAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(correlationId, out JobTrackingEntry? entry)
            && entry.ExpiresAt > _timeProvider.GetUtcNow())
        {
            return Task.FromResult<JobTrackingEntry?>(entry);
        }

        // Drop expired entries lazily on read.
        _entries.TryRemove(correlationId, out _);
        return Task.FromResult<JobTrackingEntry?>(null);
    }

    public Task RemoveAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        _entries.TryRemove(correlationId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JobTrackingEntry>> ListAsync(int limit, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        var live = _entries.Values
            .Where(e => e.ExpiresAt > now)
            .OrderBy(e => e.ExpiresAt)
            .Take(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<JobTrackingEntry>>(live);
    }
}
