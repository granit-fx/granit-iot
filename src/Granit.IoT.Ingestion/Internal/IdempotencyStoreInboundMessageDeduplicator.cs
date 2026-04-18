using Granit.Http.Idempotency.Abstractions;
using Granit.Http.Idempotency.Models;
using Granit.IoT.Diagnostics;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Options;
using Granit.Timing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Internal;

/// <summary>
/// Default deduplicator. Wraps <see cref="IIdempotencyStore"/> to perform an atomic
/// Redis SET-NX-PX on the sanitized transport message id. Fails open on Redis errors —
/// availability is preferred over strict idempotency for telemetry ingestion.
/// </summary>
/// <remarks>
/// The fail-open branch is the conscious availability/integrity trade-off: during a
/// Redis outage a replay-window worth of duplicate messages may slip through instead
/// of rejecting all traffic. Hosts with strict integrity needs must alert on sustained
/// growth of <c>granit.iot.ingestion.dedup_fail_open</c> and set
/// <c>DeduplicationWindowMinutes</c> as low as the transport retry window allows.
/// </remarks>
internal sealed partial class IdempotencyStoreInboundMessageDeduplicator(
    IIdempotencyStore store,
    IClock clock,
    IOptions<GranitIoTIngestionOptions> options,
    IoTMetrics metrics,
    ILogger<IdempotencyStoreInboundMessageDeduplicator> logger) : IInboundMessageDeduplicator
{
    private const string KeyPrefix = "iot-msg:";

    public async Task<bool> TryAcquireAsync(string sanitizedMessageId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sanitizedMessageId);

        var ttl = TimeSpan.FromMinutes(options.Value.DeduplicationWindowMinutes);
        string key = string.Concat(KeyPrefix, sanitizedMessageId);

        var entry = new IdempotencyEntry
        {
            State = IdempotencyState.InProgress,
            PayloadHash = sanitizedMessageId,
            CreatedAt = clock.Now,
        };

        try
        {
            return await store.TryAcquireAsync(key, entry, ttl, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            metrics.RecordDedupFailOpen();
            LogDeduplicationStoreUnavailable(logger, ex, key);
            return true;
        }
    }

    [LoggerMessage(EventId = 4101, Level = LogLevel.Warning, Message = "Idempotency store unavailable for key '{Key}'. Continuing without deduplication (fail-open).")]
    private static partial void LogDeduplicationStoreUnavailable(ILogger logger, Exception exception, string key);
}
