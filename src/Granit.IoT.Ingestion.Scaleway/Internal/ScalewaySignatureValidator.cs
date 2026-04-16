using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Scaleway.Options;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Scaleway.Internal;

/// <summary>
/// HMAC-SHA256 verifier for Scaleway IoT Hub webhook deliveries. Reads the shared secret
/// from <see cref="IOptionsMonitor{TOptions}"/> so secret rotations apply without a process
/// restart. Comparison uses <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
/// to thwart timing oracles.
/// </summary>
internal sealed class ScalewaySignatureValidator(
    IOptionsMonitor<ScalewayIoTOptions> options) : IPayloadSignatureValidator
{
    public string SourceName => ScalewayConstants.SourceName;

    public ValueTask<SignatureValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (!headers.TryGetValue(ScalewayConstants.SignatureHeader, out string? signatureHeader)
            || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return ValueTask.FromResult(
                SignatureValidationResult.Invalid($"Missing '{ScalewayConstants.SignatureHeader}' header."));
        }

        ScalewayIoTOptions current = options.CurrentValue;
        if (string.IsNullOrWhiteSpace(current.SharedSecret))
        {
            return ValueTask.FromResult(
                SignatureValidationResult.Invalid("Scaleway shared secret is not configured."));
        }

        if (!TryParseHexSignature(signatureHeader.Trim(), out byte[]? expected))
        {
            return ValueTask.FromResult(
                SignatureValidationResult.Invalid("Signature header is not a valid hexadecimal HMAC-SHA256 digest."));
        }

        Span<byte> computed = stackalloc byte[HMACSHA256.HashSizeInBytes];
        int written = HMACSHA256.HashData(
            key: Encoding.UTF8.GetBytes(current.SharedSecret),
            source: body.Span,
            destination: computed);

        bool match = CryptographicOperations.FixedTimeEquals(computed[..written], expected);

        return ValueTask.FromResult(match
            ? SignatureValidationResult.Valid
            : SignatureValidationResult.Invalid("HMAC-SHA256 signature mismatch."));
    }

    private static bool TryParseHexSignature(string value, out byte[]? bytes)
    {
        bytes = null;
        if (value.Length is not (HMACSHA256.HashSizeInBytes * 2) || (value.Length % 2) != 0)
        {
            return false;
        }

        byte[] buffer = new byte[HMACSHA256.HashSizeInBytes];
        for (int i = 0; i < buffer.Length; i++)
        {
            ReadOnlySpan<char> pair = value.AsSpan(i * 2, 2);
            if (!byte.TryParse(pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
            {
                return false;
            }

            buffer[i] = b;
        }

        bytes = buffer;
        return true;
    }
}
