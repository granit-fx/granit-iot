using System.Security.Cryptography;
using System.Text;

namespace Granit.IoT.Ingestion.Aws.Internal.SigV4;

/// <summary>
/// Derives a SigV4 <em>signing key</em> from a secret access key, date,
/// region, and service via the four-step HMAC-SHA256 chain defined in the
/// AWS signing spec. The signing key changes daily per region+service, so a
/// process-level cache keyed by <see cref="SigV4Scope"/> collapses thousands
/// of inbound requests onto a single key derivation.
/// </summary>
internal static class SigV4SigningKey
{
    /// <summary>
    /// Derives the signing key for <paramref name="scope"/> from the secret
    /// access key. Returns a 32-byte array.
    /// </summary>
    internal static byte[] Derive(string secretAccessKey, SigV4Scope scope)
    {
        ArgumentException.ThrowIfNullOrEmpty(secretAccessKey);

        byte[] kSecret = Encoding.UTF8.GetBytes("AWS4" + secretAccessKey);
        byte[] kDate = HMACSHA256.HashData(kSecret, Encoding.UTF8.GetBytes(scope.Date));
        byte[] kRegion = HMACSHA256.HashData(kDate, Encoding.UTF8.GetBytes(scope.Region));
        byte[] kService = HMACSHA256.HashData(kRegion, Encoding.UTF8.GetBytes(scope.Service));
        byte[] kSigning = HMACSHA256.HashData(kService, Encoding.UTF8.GetBytes(SigV4Scope.TerminatorToken));
        return kSigning;
    }

    /// <summary>
    /// Computes the final signature for a pre-built <paramref name="stringToSign"/>
    /// using the derived signing key. Returns a lowercase hex string matching
    /// the <c>Signature=</c> value sent by the client.
    /// </summary>
    internal static string Sign(byte[] signingKey, string stringToSign)
    {
        ArgumentNullException.ThrowIfNull(signingKey);
        ArgumentException.ThrowIfNullOrEmpty(stringToSign);

        Span<byte> hash = stackalloc byte[32];
        int written = HMACSHA256.HashData(
            signingKey, Encoding.UTF8.GetBytes(stringToSign), hash);
        return Convert.ToHexStringLower(hash[..written]);
    }
}
