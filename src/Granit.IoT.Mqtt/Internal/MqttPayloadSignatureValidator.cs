using Granit.IoT.Ingestion.Abstractions;

namespace Granit.IoT.Mqtt.Internal;

/// <summary>
/// "Valid by mTLS" signature validator for the <c>mqtt</c> source. The TLS handshake
/// against the broker already authenticated the publishing device via its client
/// certificate, so there is no payload-level HMAC to verify.
/// </summary>
/// <remarks>
/// This is intentionally a distinct type from
/// <c>Granit.IoT.Ingestion.Internal.NullPayloadSignatureValidator</c> (which is dev-only
/// and registered with <c>SourceName = "development"</c>) so that an architecture test
/// can assert that no production code accidentally registers the dev-only validator
/// for the <c>"mqtt"</c> source.
/// </remarks>
internal sealed class MqttPayloadSignatureValidator : IPayloadSignatureValidator
{
    public string SourceName => MqttConstants.SourceName;

    public ValueTask<SignatureValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(SignatureValidationResult.Valid);
}
