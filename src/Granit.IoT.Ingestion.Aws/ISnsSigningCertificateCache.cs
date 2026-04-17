using System.Security.Cryptography;

namespace Granit.IoT.Ingestion.Aws;

/// <summary>
/// Fetches and caches the RSA public key used to verify AWS SNS message
/// signatures. Every SNS message body embeds a <c>SigningCertURL</c> pointing
/// at a PEM certificate on the AWS CDN; fetching it per request would be a
/// self-inflicted DoS at production volume.
/// </summary>
/// <remarks>
/// Implementations MUST validate the URL against an allow-list regex
/// (only <c>https://sns.{region}.amazonaws.com/SimpleNotificationService-*.pem</c>)
/// BEFORE any HTTP call — an attacker who controls the SNS body could
/// otherwise steer the cache into fetching a cert they control.
/// </remarks>
public interface ISnsSigningCertificateCache
{
    /// <summary>
    /// Returns the RSA public key for <paramref name="certUrl"/>, fetching and
    /// caching it on first call. Throws <see cref="InvalidOperationException"/>
    /// if the URL fails the CDN allow-list, and <see cref="SnsSigningCertFetchException"/>
    /// if the fetch itself fails.
    /// </summary>
    Task<RSA> GetPublicKeyAsync(string certUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evicts the cached key for <paramref name="certUrl"/>. Call this when a
    /// signature verification fails with a cached key so the next request
    /// re-fetches — guards against an in-progress AWS cert rotation.
    /// </summary>
    void Invalidate(string certUrl);
}
