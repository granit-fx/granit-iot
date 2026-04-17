namespace Granit.IoT.Aws.Jobs.Internal;

/// <summary>
/// Stores correlationId → AWS Job id for the lifetime of an in-flight
/// command. The polling service reads from this store to know which jobs
/// to inspect, and removes the entry once a terminal status is reached.
/// The default in-memory implementation is per-host; production deployments
/// with horizontal scale-out should swap it for a Redis-backed store via
/// <c>IDistributedCache</c> (see the README example).
/// </summary>
internal interface IJobTrackingStore
{
    Task SetAsync(Guid correlationId, JobTrackingEntry entry, TimeSpan ttl, CancellationToken cancellationToken);

    Task<JobTrackingEntry?> GetAsync(Guid correlationId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid correlationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobTrackingEntry>> ListAsync(int limit, CancellationToken cancellationToken);
}

internal sealed record JobTrackingEntry(
    Guid CorrelationId,
    string JobId,
    string ThingName,
    Guid? TenantId,
    DateTimeOffset ExpiresAt);
