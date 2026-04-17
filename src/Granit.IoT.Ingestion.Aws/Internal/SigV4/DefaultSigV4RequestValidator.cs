using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Granit.IoT.Ingestion.Aws.Internal.SigV4;

/// <summary>
/// Default implementation of <see cref="ISigV4RequestValidator"/>. Parses the
/// <c>Authorization</c> header, rebuilds the canonical request + string to
/// sign, derives the signing key (once per day per credential scope, cached
/// in <see cref="IFusionCache"/>), and compares the client signature to the
/// computed one with <see cref="CryptographicOperations.FixedTimeEquals(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})"/>.
/// </summary>
internal sealed class DefaultSigV4RequestValidator(
    ISigV4SigningKeyProvider keyProvider,
    IFusionCache cache,
    IOptionsMonitor<AwsIoTIngestionOptions> options,
    TimeProvider timeProvider)
    : ISigV4RequestValidator
{
    internal const string AmzDateHeader = "x-amz-date";
    internal const string AuthorizationHeader = "authorization";

    /// <summary>Clock skew tolerance (matches AWS SigV4 spec).</summary>
    internal static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(5);

    private const string SigningKeyCachePrefix = "iotaws:sigv4:key:";

    public async ValueTask<SignatureValidationResult> ValidateAsync(
        string method,
        string canonicalUri,
        string canonicalQueryString,
        IReadOnlyDictionary<string, string> headers,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentNullException.ThrowIfNull(canonicalUri);
        ArgumentNullException.ThrowIfNull(canonicalQueryString);
        ArgumentNullException.ThrowIfNull(headers);

        if (!headers.TryGetValue(AuthorizationHeader, out string? authorization))
        {
            return SignatureValidationResult.Invalid("Missing Authorization header.");
        }

        var parsed = SigV4AuthorizationHeader.TryParse(authorization);
        if (parsed is null)
        {
            return SignatureValidationResult.Invalid(
                "Authorization header is not a valid AWS4-HMAC-SHA256 directive.");
        }

        if (!headers.TryGetValue(AmzDateHeader, out string? amzDate)
            || string.IsNullOrWhiteSpace(amzDate))
        {
            return SignatureValidationResult.Invalid($"Missing '{AmzDateHeader}' header.");
        }

        if (!SigV4CanonicalRequestBuilder.TryParseAmzDate(amzDate, out DateTimeOffset requestTime))
        {
            return SignatureValidationResult.Invalid(
                $"'{AmzDateHeader}' header is not a valid AWS timestamp (yyyyMMddTHHmmssZ).");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (Math.Abs((now - requestTime).TotalSeconds) > ClockSkewTolerance.TotalSeconds)
        {
            return SignatureValidationResult.Invalid(
                "Request timestamp is outside the 5-minute clock-skew window; rejecting as replay/stale.");
        }

        // The scope's date must match the x-amz-date day — guards against a
        // client signing with yesterday's scope and replaying today.
        if (!string.Equals(
                parsed.Scope.Date,
                requestTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            return SignatureValidationResult.Invalid(
                "Credential scope date does not match x-amz-date.");
        }

        string? secretAccessKey = await keyProvider
            .GetSecretAccessKeyAsync(parsed.AccessKeyId, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrEmpty(secretAccessKey))
        {
            // Fail-closed: an unknown AKID is an authn failure, not a 500.
            return SignatureValidationResult.Invalid(
                $"Unknown access key id '{parsed.AccessKeyId}'.");
        }

        byte[] signingKey = await GetOrDeriveSigningKeyAsync(
            parsed.AccessKeyId, secretAccessKey, parsed.Scope, cancellationToken)
            .ConfigureAwait(false);

        string canonicalRequest = SigV4CanonicalRequestBuilder.BuildCanonicalRequest(
            method, canonicalUri, canonicalQueryString, headers, parsed.SignedHeaders, body);
        string stringToSign = SigV4CanonicalRequestBuilder.BuildStringToSign(
            amzDate, parsed.Scope, canonicalRequest);
        string computedSignature = SigV4SigningKey.Sign(signingKey, stringToSign);

        bool match = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedSignature),
            Encoding.ASCII.GetBytes(parsed.Signature));

        return match
            ? SignatureValidationResult.Valid
            : SignatureValidationResult.Invalid("SigV4 signature mismatch.");
    }

    private Task<byte[]> GetOrDeriveSigningKeyAsync(
        string accessKeyId,
        string secretAccessKey,
        SigV4Scope scope,
        CancellationToken cancellationToken)
    {
        string key = $"{SigningKeyCachePrefix}{accessKeyId}:{scope}";
        int ttlHours = Math.Max(1, options.CurrentValue.SigningKeyCacheHours);

        return cache.GetOrSetAsync<byte[]>(
            key,
            factory: (_, _) => Task.FromResult(SigV4SigningKey.Derive(secretAccessKey, scope)),
            options: new FusionCacheEntryOptions(TimeSpan.FromHours(ttlHours)),
            token: cancellationToken).AsTask();
    }
}
