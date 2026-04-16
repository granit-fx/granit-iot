using System.Diagnostics;
using System.Text.RegularExpressions;
using Granit.Events;
using Granit.IoT.Abstractions;
using Granit.IoT.Diagnostics;
using Granit.IoT.Events;
using Granit.IoT.Ingestion.Abstractions;
using Granit.Timing;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Ingestion.Internal;

/// <summary>
/// Default ingestion pipeline. Resolves the right provider by <c>SourceName</c>, validates
/// the signature, parses the payload, deduplicates by transport message id, resolves the
/// device, then dispatches a <see cref="TelemetryIngestedEto"/> (or <see cref="DeviceUnknownEto"/>)
/// to the outbox via <see cref="IDistributedEventBus"/>.
/// </summary>
internal sealed partial class IngestionPipeline(
    IEnumerable<IInboundMessageParser> parsers,
    IEnumerable<IPayloadSignatureValidator> signatureValidators,
    IDeviceLookup deviceLookup,
    IInboundMessageDeduplicator deduplicator,
    IDistributedEventBus eventBus,
    IClock clock,
    IoTMetrics metrics,
    ILogger<IngestionPipeline> logger) : IIngestionPipeline
{
    private readonly Dictionary<string, IInboundMessageParser> _parsersBySource =
        parsers.ToDictionary(p => p.SourceName, StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IPayloadSignatureValidator> _validatorsBySource =
        signatureValidators.ToDictionary(v => v.SourceName, StringComparer.OrdinalIgnoreCase);

    public async Task<IngestionResult> ProcessAsync(
        string source,
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(headers);

        using Activity? activity = IoTActivitySource.Source.StartActivity("iot.ingestion.process");
        activity?.SetTag("iot.source", source);

        if (!_parsersBySource.TryGetValue(source, out IInboundMessageParser? parser) ||
            !_validatorsBySource.TryGetValue(source, out IPayloadSignatureValidator? validator))
        {
            LogUnknownSource(logger, source);
            return IngestionResult.UnknownSource(source);
        }

        SignatureValidationResult signature = await validator.ValidateAsync(body, headers, cancellationToken).ConfigureAwait(false);
        if (!signature.IsValid)
        {
            metrics.RecordIngestionSignatureRejected(tenantId: null, source);
            LogSignatureRejected(logger, source, signature.FailureReason ?? "unspecified");
            return IngestionResult.SignatureRejected(signature.FailureReason ?? "Invalid signature.");
        }

        ParsedTelemetryBatch batch;
        try
        {
            batch = await parser.ParseAsync(body, cancellationToken).ConfigureAwait(false);
        }
        catch (IngestionParseException ex)
        {
            LogParseFailure(logger, source, ex.Message);
            return IngestionResult.ParseFailure(ex.Message);
        }

        string sanitizedMessageId = SanitizeMessageId(batch.MessageId);
        bool acquired = await deduplicator.TryAcquireAsync(sanitizedMessageId, cancellationToken).ConfigureAwait(false);
        if (!acquired)
        {
            metrics.RecordIngestionDuplicateSkipped(tenantId: null, source);
            LogDuplicateSkipped(logger, source, sanitizedMessageId);
            return IngestionResult.Accepted;
        }

        DeviceLookupResult? device = await deviceLookup
            .FindBySerialNumberAsync(batch.DeviceExternalId, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            metrics.RecordIngestionUnknownDevice(tenantId: null, source);
            LogUnknownDevice(logger, source, batch.DeviceExternalId);

            await eventBus.PublishAsync(
                new DeviceUnknownEto(
                    batch.MessageId,
                    batch.DeviceExternalId,
                    source,
                    clock.Now),
                cancellationToken).ConfigureAwait(false);

            return IngestionResult.Accepted;
        }

        await eventBus.PublishAsync(
            new TelemetryIngestedEto(
                batch.MessageId,
                batch.DeviceExternalId,
                device.DeviceId,
                device.TenantId,
                batch.RecordedAt,
                batch.Metrics,
                source,
                batch.Tags),
            cancellationToken).ConfigureAwait(false);

        return IngestionResult.Accepted;
    }

    internal static string SanitizeMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        string trimmed = messageId.Trim();
        if (trimmed.Length > 128)
        {
            trimmed = trimmed[..128];
        }

        return MessageIdPattern().Replace(trimmed, "-");
    }

    [GeneratedRegex(@"[^a-zA-Z0-9\-]+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MessageIdPattern();

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "No ingestion provider registered for source '{Source}'.")]
    private static partial void LogUnknownSource(ILogger logger, string source);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Signature rejected for source '{Source}': {Reason}.")]
    private static partial void LogSignatureRejected(ILogger logger, string source, string reason);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Warning, Message = "Failed to parse inbound payload for source '{Source}': {Reason}.")]
    private static partial void LogParseFailure(ILogger logger, string source, string reason);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Duplicate ingestion skipped for source '{Source}' (message id '{MessageId}').")]
    private static partial void LogDuplicateSkipped(ILogger logger, string source, string messageId);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Information, Message = "Unknown device '{DeviceExternalId}' on source '{Source}' — emitting DeviceUnknownEto.")]
    private static partial void LogUnknownDevice(ILogger logger, string source, string deviceExternalId);
}
