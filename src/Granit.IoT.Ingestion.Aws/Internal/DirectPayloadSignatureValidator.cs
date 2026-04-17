using System.Security.Cryptography;
using System.Text;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws.Diagnostics;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Validates inbound deliveries on the AWS IoT Core <em>direct HTTP</em>
/// path. Dual-mode: in Production the rule must sign every request with
/// SigV4; in Development a static Bearer API key can be used instead. The
/// options validator rejects a non-null API key outside <c>Development</c>
/// at startup so a leaked secret in appsettings never silently ships.
/// </summary>
internal sealed class DirectPayloadSignatureValidator(
    ISigV4RequestValidator sigV4Validator,
    IOptionsMonitor<AwsIoTIngestionOptions> options,
    AwsIoTIngestionMetrics metrics)
    : IPayloadSignatureValidator
{
    private const string BearerScheme = "Bearer ";

    public string SourceName => AwsIoTIngestionConstants.DirectSourceName;

    public async ValueTask<SignatureValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(headers);

        DirectIngestionOptions current = options.CurrentValue.Direct;
        if (!current.Enabled)
        {
            return Reject("AWS IoT direct ingestion path is disabled.");
        }

        SignatureValidationResult outcome = current.AuthMode switch
        {
            DirectAuthMode.SigV4 => await ValidateSigV4Async(body, headers, cancellationToken).ConfigureAwait(false),
            DirectAuthMode.ApiKey => ValidateApiKey(headers, current.ApiKey),
            _ => SignatureValidationResult.Invalid("Unknown Direct.AuthMode."),
        };

        if (outcome.IsValid)
        {
            metrics.SigV4Accepted.Add(1);
        }
        else
        {
            metrics.SigV4Rejected.Add(1);
        }

        return outcome;
    }

    private async Task<SignatureValidationResult> ValidateSigV4Async(
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestMetadata(headers, out string method, out string path, out string query))
        {
            return SignatureValidationResult.Invalid(
                "SigV4 validation requires server-controlled request metadata (granit-request-*) — " +
                "route AWS direct traffic through the standard ingestion endpoint.");
        }

        return await sigV4Validator.ValidateAsync(
            method, path, query, headers, body, cancellationToken).ConfigureAwait(false);
    }

    private static SignatureValidationResult ValidateApiKey(
        IReadOnlyDictionary<string, string> headers,
        string? expectedApiKey)
    {
        if (string.IsNullOrEmpty(expectedApiKey))
        {
            return SignatureValidationResult.Invalid(
                "Direct.AuthMode=ApiKey but no ApiKey is configured — resolve it from Granit.Vault.");
        }

        if (!headers.TryGetValue("Authorization", out string? authorization)
            || string.IsNullOrWhiteSpace(authorization)
            || !authorization.StartsWith(BearerScheme, StringComparison.Ordinal))
        {
            return SignatureValidationResult.Invalid("Missing or malformed Bearer token.");
        }

        string presented = authorization[BearerScheme.Length..];
        bool match = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(presented),
            Encoding.UTF8.GetBytes(expectedApiKey));

        return match
            ? SignatureValidationResult.Valid
            : SignatureValidationResult.Invalid("Bearer token mismatch.");
    }

    internal static bool TryGetRequestMetadata(
        IReadOnlyDictionary<string, string> headers,
        out string method,
        out string path,
        out string query)
    {
        method = string.Empty;
        path = string.Empty;
        query = string.Empty;

        if (!headers.TryGetValue(IngestionRequestHeaders.Method, out string? m)
            || !headers.TryGetValue(IngestionRequestHeaders.Path, out string? p))
        {
            return false;
        }

        method = m;
        path = p;
        query = headers.TryGetValue(IngestionRequestHeaders.Query, out string? q) ? q : string.Empty;
        return true;
    }

    private SignatureValidationResult Reject(string reason)
    {
        metrics.SigV4Rejected.Add(1);
        return SignatureValidationResult.Invalid(reason);
    }
}
