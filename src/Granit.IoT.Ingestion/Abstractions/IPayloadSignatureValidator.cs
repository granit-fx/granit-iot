namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Verifies the cryptographic signature of an inbound webhook payload.
/// Implementations are resolved by matching <see cref="SourceName"/> to the route segment
/// <c>{source}</c> of the ingestion endpoint.
/// </summary>
public interface IPayloadSignatureValidator
{
    /// <summary>
    /// Provider source key (e.g. <c>"scaleway"</c>). Must be lowercase.
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Validates the provider-supplied signature against the raw HTTP body.
    /// </summary>
    /// <param name="body">Raw HTTP body bytes (must be the original wire bytes).</param>
    /// <param name="headers">Inbound request headers (case-insensitive).</param>
    ValueTask<SignatureValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}
