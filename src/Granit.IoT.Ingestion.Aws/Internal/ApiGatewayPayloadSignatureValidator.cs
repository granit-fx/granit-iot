using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws.Diagnostics;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Validates inbound deliveries on the AWS IoT Core → API Gateway → HTTP
/// path. API Gateway always signs with SigV4 — no Bearer mode exists here,
/// and there is no fallback. Everything delegates to
/// <see cref="ISigV4RequestValidator"/> after pulling the HTTP metadata
/// stamped on the request by the ingestion endpoint.
/// </summary>
internal sealed class ApiGatewayPayloadSignatureValidator(
    ISigV4RequestValidator sigV4Validator,
    IOptionsMonitor<AwsIoTIngestionOptions> options,
    AwsIoTIngestionMetrics metrics)
    : IPayloadSignatureValidator
{
    public string SourceName => AwsIoTIngestionConstants.ApiGatewaySourceName;

    public async ValueTask<SignatureValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (!options.CurrentValue.ApiGateway.Enabled)
        {
            metrics.SigV4Rejected.Add(1);
            return SignatureValidationResult.Invalid("AWS IoT API Gateway ingestion path is disabled.");
        }

        if (!DirectPayloadSignatureValidator.TryGetRequestMetadata(
                headers, out string method, out string path, out string query))
        {
            metrics.SigV4Rejected.Add(1);
            return SignatureValidationResult.Invalid(
                "SigV4 validation requires server-controlled request metadata (granit-request-*) — " +
                "route AWS API Gateway traffic through the standard ingestion endpoint.");
        }

        SignatureValidationResult outcome = await sigV4Validator
            .ValidateAsync(method, path, query, headers, body, cancellationToken)
            .ConfigureAwait(false);

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
}
