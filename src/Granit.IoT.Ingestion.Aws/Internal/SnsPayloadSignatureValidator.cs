using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws.Diagnostics;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Validates AWS SNS HTTP-subscription deliveries: confirms the RSA-SHA256
/// signature against the fetched AWS signing certificate, blocks replays via
/// <c>MessageId</c> deduplication, enforces the <c>TopicArn</c> allow-list,
/// and auto-confirms <c>SubscriptionConfirmation</c> messages when the option
/// is enabled.
/// </summary>
internal sealed partial class SnsPayloadSignatureValidator(
    ISnsSigningCertificateCache certCache,
    IOptionsMonitor<AwsIoTIngestionOptions> options,
    IFusionCache dedupCache,
    IHttpClientFactory httpClientFactory,
    IoTIngestionAwsMetrics metrics,
    ILogger<SnsPayloadSignatureValidator> logger)
    : IPayloadSignatureValidator
{
    internal const string SubscribeHttpClientName = "AwsIoTSnsSubscribeConfirm";

    private const string DedupCacheKeyPrefix = "iotaws:sns:msgid:";

    public string SourceName => AwsIoTIngestionConstants.SnsSourceName;

    public async ValueTask<SignatureValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        SnsEnvelope? envelope = DeserializeEnvelope(body);
        if (envelope is null)
        {
            return Reject("SNS envelope is not valid JSON.");
        }

        if (string.IsNullOrEmpty(envelope.Type)
            || string.IsNullOrEmpty(envelope.MessageId)
            || string.IsNullOrEmpty(envelope.Signature)
            || string.IsNullOrEmpty(envelope.SigningCertUrl)
            || string.IsNullOrEmpty(envelope.TopicArn))
        {
            return Reject("SNS envelope is missing required fields.");
        }

        AwsIoTSnsIngestionOptions snsOptions = options.CurrentValue.Sns;
        if (!snsOptions.Enabled)
        {
            return Reject("SNS ingestion path is disabled.");
        }

        if (!string.IsNullOrEmpty(snsOptions.TopicArnPrefix)
            && !envelope.TopicArn.StartsWith(snsOptions.TopicArnPrefix, StringComparison.Ordinal))
        {
            return Reject($"TopicArn '{envelope.TopicArn}' does not match the configured prefix.");
        }

        // Dedup before any crypto work — cheapest check.
        if (await IsReplayAsync(envelope.MessageId, snsOptions.DeduplicationWindowMinutes, cancellationToken)
            .ConfigureAwait(false))
        {
            metrics.SnsReplays.Add(1);
            return Reject($"SNS MessageId '{envelope.MessageId}' already seen within the replay window.");
        }

        string? canonical = SnsCanonicalStringBuilder.Build(envelope);
        if (canonical is null)
        {
            return Reject("SNS envelope is missing fields required to build the canonical string.");
        }

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(envelope.Signature!);
        }
        catch (FormatException)
        {
            return Reject("SNS Signature is not valid base64.");
        }

        RSA publicKey;
        try
        {
            publicKey = await certCache.GetPublicKeyAsync(envelope.SigningCertUrl!, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            // Thrown when the cert URL fails the CDN allow-list — treat as invalid,
            // do NOT escalate to 503.
            return Reject($"SNS SigningCertURL rejected: {ex.Message}");
        }

        bool verified = publicKey.VerifyData(
            Encoding.UTF8.GetBytes(canonical),
            signatureBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        if (!verified)
        {
            // Could be a key rotation — invalidate and let the next call re-fetch.
            certCache.Invalidate(envelope.SigningCertUrl!);
            return Reject("SNS RSA-SHA256 signature verification failed.");
        }

        if (envelope.Type == SnsEnvelope.MessageTypes.SubscriptionConfirmation)
        {
            await HandleSubscriptionConfirmationAsync(envelope, snsOptions, cancellationToken).ConfigureAwait(false);
            metrics.SnsSubscriptionConfirmations.Add(1);
            return SignatureValidationResult.Valid;
        }

        metrics.SnsAccepted.Add(1);
        return SignatureValidationResult.Valid;
    }

    private async Task<bool> IsReplayAsync(string messageId, int windowMinutes, CancellationToken cancellationToken)
    {
        string key = DedupCacheKeyPrefix + messageId;

        MaybeValue<byte> existing = await dedupCache
            .TryGetAsync<byte>(key, token: cancellationToken)
            .ConfigureAwait(false);
        if (existing.HasValue)
        {
            return true;
        }

        // The TryGet/Set pair leaves a microsecond-scale TOCTOU window during
        // which two concurrent deliveries of the same SNS MessageId can both
        // pass the replay check. The serialization lock below closes the
        // race for same-process callers; across processes the broker's own
        // at-least-once delivery SLA is the remaining boundary, bounded by
        // windowMinutes.
        SemaphoreSlim gate = _dedupGates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            existing = await dedupCache
                .TryGetAsync<byte>(key, token: cancellationToken)
                .ConfigureAwait(false);
            if (existing.HasValue)
            {
                return true;
            }

            await dedupCache.SetAsync<byte>(
                key,
                1,
                options: new FusionCacheEntryOptions(TimeSpan.FromMinutes(windowMinutes)),
                token: cancellationToken).ConfigureAwait(false);
            return false;
        }
        finally
        {
            gate.Release();
            // Drop the lock entry after the TTL has passed so the dictionary
            // doesn't grow unbounded — `TryRemove` is cheap and the race of
            // another arrival during drop just creates a new semaphore.
            _dedupGates.TryRemove(new KeyValuePair<string, SemaphoreSlim>(key, gate));
        }
    }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _dedupGates = new();

    private async Task HandleSubscriptionConfirmationAsync(
        SnsEnvelope envelope,
        AwsIoTSnsIngestionOptions snsOptions,
        CancellationToken cancellationToken)
    {
        if (!snsOptions.AutoConfirmSubscription)
        {
            LogManualConfirmationRequired(logger, envelope.TopicArn!, envelope.SubscribeUrl ?? "<missing>");
            return;
        }

        if (string.IsNullOrEmpty(envelope.SubscribeUrl))
        {
            LogManualConfirmationRequired(logger, envelope.TopicArn!, "<missing>");
            return;
        }

        // Pin the SubscribeURL to the AWS SNS CDN. SNS never signs the
        // SubscribeURL itself — a forged envelope that passes RSA verification
        // could still point at an attacker-controlled host. Matching the same
        // allow-list used for the signing certificate closes the SSRF window.
        if (!SubscribeUrlPattern().IsMatch(envelope.SubscribeUrl))
        {
            LogSubscribeUrlRejected(logger, envelope.TopicArn!, envelope.SubscribeUrl);
            return;
        }

        HttpClient client = httpClientFactory.CreateClient(SubscribeHttpClientName);
        try
        {
            using HttpResponseMessage response = await client
                .GetAsync(envelope.SubscribeUrl, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            LogSubscriptionConfirmed(logger, envelope.TopicArn!);
        }
        catch (HttpRequestException ex)
        {
            // The SubscribeURL GET is best-effort. Log and move on — AWS will retry
            // by resending the confirmation message.
            LogSubscriptionConfirmationFailed(logger, envelope.TopicArn!, ex);
        }
    }

    private SignatureValidationResult Reject(string reason)
    {
        metrics.SnsRejected.Add(1);
        return SignatureValidationResult.Invalid(reason);
    }

    private static SnsEnvelope? DeserializeEnvelope(ReadOnlyMemory<byte> body)
    {
        try
        {
            return JsonSerializer.Deserialize<SnsEnvelope>(body.Span, IngestionJsonOptions.Default);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "AWS SNS subscription auto-confirmed for topic {TopicArn}.")]
    private static partial void LogSubscriptionConfirmed(ILogger logger, string topicArn);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "AWS SNS SubscriptionConfirmation received for topic {TopicArn} but auto-confirm is disabled or SubscribeURL missing (url={SubscribeUrl}). Confirm manually to start receiving notifications.")]
    private static partial void LogManualConfirmationRequired(ILogger logger, string topicArn, string subscribeUrl);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Failed to GET SNS SubscribeURL for topic {TopicArn}; AWS will retry the confirmation.")]
    private static partial void LogSubscriptionConfirmationFailed(ILogger logger, string topicArn, Exception exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Refusing to auto-confirm SNS subscription for topic {TopicArn}: SubscribeURL '{SubscribeUrl}' is not on the AWS SNS CDN allow-list.")]
    private static partial void LogSubscribeUrlRejected(ILogger logger, string topicArn, string subscribeUrl);

    [GeneratedRegex(
        @"^https://sns\.[a-z0-9\-]+\.amazonaws\.com/\?Action=ConfirmSubscription(&.+)?$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex SubscribeUrlPattern();
}
