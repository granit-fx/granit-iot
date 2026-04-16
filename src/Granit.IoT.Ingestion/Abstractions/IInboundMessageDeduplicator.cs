namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Best-effort deduplication of inbound webhook deliveries keyed on transport message id.
/// </summary>
/// <remarks>
/// Implementations should rely on a Redis-backed atomic SET-NX with TTL primitive
/// (typically <c>Granit.Http.Idempotency.IIdempotencyStore.TryAcquireAsync</c>).
/// When the backing store is unavailable, implementations should fail open
/// (return <see langword="true"/>) — availability over idempotency.
/// </remarks>
public interface IInboundMessageDeduplicator
{
    /// <summary>
    /// Attempts to register the given message id as seen.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if this is the first observation (continue processing);
    /// <see langword="false"/> if the message id was already seen within the deduplication
    /// window (skip processing).
    /// </returns>
    Task<bool> TryAcquireAsync(string sanitizedMessageId, CancellationToken cancellationToken);
}
