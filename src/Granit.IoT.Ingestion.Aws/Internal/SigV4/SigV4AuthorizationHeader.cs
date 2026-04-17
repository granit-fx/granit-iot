namespace Granit.IoT.Ingestion.Aws.Internal.SigV4;

/// <summary>
/// Parsed form of the <c>Authorization</c> header for
/// <c>AWS4-HMAC-SHA256</c>. Example:
/// <c>AWS4-HMAC-SHA256 Credential=AKID/20260417/eu-west-1/iotdata/aws4_request,
/// SignedHeaders=host;x-amz-date, Signature=abcd...</c>.
/// </summary>
internal sealed record SigV4AuthorizationHeader(
    string AccessKeyId,
    SigV4Scope Scope,
    IReadOnlyList<string> SignedHeaders,
    string Signature)
{
#pragma warning disable GRSEC003 // AWS protocol literal, not a secret.
    internal const string AlgorithmToken = "AWS4-HMAC-SHA256";
#pragma warning restore GRSEC003

    /// <summary>
    /// Parses the header value. Returns <c>null</c> if the header is missing
    /// components, in the wrong shape, or references an unsupported algorithm.
    /// </summary>
    internal static SigV4AuthorizationHeader? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Algorithm token up to the first space, then a comma-separated parameter list.
        int firstSpace = value.IndexOf(' ', StringComparison.Ordinal);
        if (firstSpace < 0)
        {
            return null;
        }

        string algorithm = value[..firstSpace];
        if (!string.Equals(algorithm, AlgorithmToken, StringComparison.Ordinal))
        {
            return null;
        }

        string? credential = null;
        string? signedHeaders = null;
        string? signature = null;

        foreach (string rawPart in value[(firstSpace + 1)..].Split(','))
        {
            string part = rawPart.Trim();
            if (TryReadParam(part, "Credential=", out string? value1))
            {
                credential = value1;
            }
            else if (TryReadParam(part, "SignedHeaders=", out string? value2))
            {
                signedHeaders = value2;
            }
            else if (TryReadParam(part, "Signature=", out string? value3))
            {
                signature = value3;
            }
        }

        if (string.IsNullOrEmpty(credential)
            || string.IsNullOrEmpty(signedHeaders)
            || string.IsNullOrEmpty(signature))
        {
            return null;
        }

        // Credential = AKID/yyyyMMdd/region/service/aws4_request
        string[] credentialParts = credential.Split('/');
        if (credentialParts.Length != 5)
        {
            return null;
        }

        if (!SigV4Scope.TryParse(
            string.Join('/', credentialParts[1], credentialParts[2], credentialParts[3], credentialParts[4]),
            out SigV4Scope scope))
        {
            return null;
        }

        string[] signedHeaderNames = signedHeaders.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(h => h.ToLowerInvariant())
            .ToArray();

        return new SigV4AuthorizationHeader(
            AccessKeyId: credentialParts[0],
            Scope: scope,
            SignedHeaders: signedHeaderNames,
            Signature: signature);
    }

    private static bool TryReadParam(string part, string key, out string? value)
    {
        value = null;
        if (part.StartsWith(key, StringComparison.Ordinal))
        {
            value = part[key.Length..];
            return true;
        }
        return false;
    }
}
