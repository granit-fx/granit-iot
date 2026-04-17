using System.Globalization;

namespace Granit.IoT.Ingestion.Aws.Internal.SigV4;

/// <summary>
/// The credential scope a SigV4 signature was issued under, of the shape
/// <c>{yyyyMMdd}/{region}/{service}/aws4_request</c>. Used both as a cache key
/// for the derived signing key and as the middle line of <c>StringToSign</c>.
/// </summary>
internal readonly record struct SigV4Scope(string Date, string Region, string Service)
{
#pragma warning disable GRSEC003 // AWS protocol literal, not a secret.
    internal const string TerminatorToken = "aws4_request";
#pragma warning restore GRSEC003

    /// <summary>Canonical string form: <c>yyyyMMdd/region/service/aws4_request</c>.</summary>
    public override string ToString() => $"{Date}/{Region}/{Service}/{TerminatorToken}";

    /// <summary>
    /// Parses a scope string. Returns <c>false</c> on any shape deviation — the
    /// caller must then reject the request (this is a defence-in-depth check;
    /// the <c>Authorization</c> header parser has already rejected gross noise).
    /// </summary>
    internal static bool TryParse(string value, out SigV4Scope scope)
    {
        scope = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Split('/');
        if (parts.Length != 4
            || !string.Equals(parts[3], TerminatorToken, StringComparison.Ordinal)
            || parts[0].Length != 8
            || string.IsNullOrWhiteSpace(parts[1])
            || string.IsNullOrWhiteSpace(parts[2])
            || !DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return false;
        }

        scope = new SigV4Scope(parts[0], parts[1], parts[2]);
        return true;
    }
}
