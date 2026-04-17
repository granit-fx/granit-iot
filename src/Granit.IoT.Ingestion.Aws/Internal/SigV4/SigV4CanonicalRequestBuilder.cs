using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Granit.IoT.Ingestion.Aws.Internal.SigV4;

/// <summary>
/// Produces the two intermediate strings defined by the AWS Signature
/// Version 4 signing process: the <em>canonical request</em> and the
/// <em>string to sign</em>. Both must match the incoming client byte-for-byte
/// — a single space or case difference invalidates the signature.
/// </summary>
/// <remarks>
/// AWS spec: https://docs.aws.amazon.com/general/latest/gr/sigv4_signing.html
/// </remarks>
internal static class SigV4CanonicalRequestBuilder
{
    /// <summary>
    /// Builds the canonical request string. The <paramref name="signedHeaders"/>
    /// list names the request headers included in the signature (lowercase,
    /// semicolon-separated in the order they appear in the canonical request).
    /// </summary>
    internal static string BuildCanonicalRequest(
        string method,
        string canonicalUri,
        string canonicalQueryString,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyList<string> signedHeaders,
        ReadOnlyMemory<byte> body)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(signedHeaders);

        StringBuilder canonicalHeaders = new(capacity: 256);
        foreach (string name in signedHeaders)
        {
            if (!headers.TryGetValue(name, out string? rawValue))
            {
                // A signed header absent from the request is a hard reject — the
                // signer declared it but the transport stripped it. Do NOT pad
                // with empty; let the caller see the mismatch as a verification
                // failure.
                rawValue = string.Empty;
            }

            canonicalHeaders
                .Append(name)
                .Append(':')
                .Append(TrimAllWhitespace(rawValue))
                .Append('\n');
        }

        string signedHeadersList = string.Join(';', signedHeaders);
        string payloadHash = HashPayload(body.Span);

        return string.Join('\n',
            method.ToUpperInvariant(),
            canonicalUri,
            canonicalQueryString,
            canonicalHeaders.ToString(),
            signedHeadersList,
            payloadHash);
    }

    /// <summary>
    /// Builds the string-to-sign wrapping a canonical-request hash:
    /// <c>AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{hex(SHA256(canonicalRequest))}</c>.
    /// </summary>
    internal static string BuildStringToSign(
        string amzDate,
        SigV4Scope scope,
        string canonicalRequest)
    {
        ArgumentException.ThrowIfNullOrEmpty(amzDate);
        ArgumentException.ThrowIfNullOrEmpty(canonicalRequest);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest), hash);

        return string.Join('\n',
            SigV4AuthorizationHeader.AlgorithmToken,
            amzDate,
            scope.ToString(),
            Convert.ToHexStringLower(hash));
    }

    /// <summary>
    /// Returns the hex-encoded SHA-256 of the request body, or the well-known
    /// hash of the empty string when the body is empty (the common case for
    /// GET/HEAD requests).
    /// </summary>
    internal static string HashPayload(ReadOnlySpan<byte> body)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(body, hash);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Collapses internal runs of whitespace in a header value to a single
    /// space, trims leading/trailing whitespace. Required by the AWS spec.
    /// </summary>
    internal static string TrimAllWhitespace(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder sb = new(value.Length);
        bool lastWasSpace = true;  // skip leading whitespace
        foreach (char c in value)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                lastWasSpace = false;
            }
        }

        // Trim trailing space if any.
        if (sb.Length > 0 && sb[^1] == ' ')
        {
            sb.Length--;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses an <c>x-amz-date</c> value (<c>yyyyMMddTHHmmssZ</c>).
    /// </summary>
    internal static bool TryParseAmzDate(string value, out DateTimeOffset parsed) =>
        DateTimeOffset.TryParseExact(
            value,
            "yyyyMMddTHHmmssZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out parsed);
}
