using Granit.IoT.Ingestion.Abstractions;

namespace Granit.IoT.Ingestion.Internal;

/// <summary>
/// No-op signature validator used during local development. Always returns
/// <see cref="SignatureValidationResult.Valid"/>. Registered conditionally — the DI
/// extension only adds it when <c>IHostEnvironment.IsDevelopment()</c> is <see langword="true"/>.
/// </summary>
internal sealed class NullPayloadSignatureValidator : IPayloadSignatureValidator
{
    public const string DevelopmentSourceName = "development";

    public string SourceName => DevelopmentSourceName;

    public ValueTask<SignatureValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(SignatureValidationResult.Valid);
}
