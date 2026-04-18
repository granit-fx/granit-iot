using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Default <see cref="ISnsSigningCertificateCache"/>. Fetches AWS SNS signing
/// certificates via a named <see cref="HttpClient"/> (<c>AwsIoTSnsCertFetch</c>)
/// and caches the resulting RSA public key in <see cref="IFusionCache"/> for the
/// TTL configured by <see cref="AwsIoTSnsIngestionOptions.CertCacheHours"/>. The cert
/// URL is validated against an AWS CDN allow-list regex before any HTTP call,
/// blocking the cert-injection attack where an attacker-controlled SNS body
/// points at an attacker-controlled PEM file.
/// </summary>
internal sealed partial class DefaultSnsSigningCertificateCache(
    IFusionCache cache,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<AwsIoTIngestionOptions> options,
    ILogger<DefaultSnsSigningCertificateCache> logger)
    : ISnsSigningCertificateCache
{
    internal const string HttpClientName = "AwsIoTSnsCertFetch";

    private const string CacheKeyPrefix = "iotaws:sns:cert:";

    public async Task<RSA> GetPublicKeyAsync(string certUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certUrl);

        if (!CdnUrlPattern().IsMatch(certUrl))
        {
            throw new InvalidOperationException(
                $"SNS SigningCertURL '{certUrl}' does not match the AWS CDN allow-list " +
                "(https://sns.{region}.amazonaws.com/SimpleNotificationService-*.pem). " +
                "Refusing to fetch to prevent cert-injection attacks.");
        }

        string key = CacheKey(certUrl);
        int ttlHours = options.CurrentValue.Sns.CertCacheHours;

        return await cache.GetOrSetAsync<RSA>(
            key,
            factory: async (_, ct) => await FetchPemAsync(certUrl, ct).ConfigureAwait(false),
            options: new FusionCacheEntryOptions(TimeSpan.FromHours(ttlHours)),
            token: cancellationToken).ConfigureAwait(false);
    }

    public void Invalidate(string certUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certUrl);
        cache.Remove(CacheKey(certUrl));
    }

    private async Task<RSA> FetchPemAsync(string certUrl, CancellationToken cancellationToken)
    {
        HttpClient client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            byte[] pemBytes = await client
                .GetByteArrayAsync(certUrl, cancellationToken)
                .ConfigureAwait(false);

            string pem = Encoding.ASCII.GetString(pemBytes);
            using var cert = X509Certificate2.CreateFromPem(pem);

            RSA? rsa = cert.GetRSAPublicKey();
            if (rsa is null)
            {
                throw new SnsSigningCertFetchException(
                    $"SNS certificate at '{certUrl}' does not contain an RSA public key.");
            }

            LogCertFetched(logger, certUrl);
            return rsa;
        }
        catch (HttpRequestException ex)
        {
            throw new SnsSigningCertFetchException(
                $"Failed to fetch SNS signing certificate from '{certUrl}': {ex.Message}", ex);
        }
        catch (CryptographicException ex)
        {
            throw new SnsSigningCertFetchException(
                $"SNS signing certificate at '{certUrl}' is not a valid PEM-encoded X.509 certificate: {ex.Message}",
                ex);
        }
    }

    private static string CacheKey(string certUrl)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(certUrl), hash);
        return CacheKeyPrefix + Convert.ToHexStringLower(hash);
    }

    [GeneratedRegex(
        @"^https://sns\.[a-z0-9\-]+\.amazonaws\.com/SimpleNotificationService-[A-Za-z0-9]+\.pem$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex CdnUrlPattern();

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Fetched AWS SNS signing certificate from {CertUrl} and cached RSA public key.")]
    private static partial void LogCertFetched(ILogger logger, string certUrl);
}
