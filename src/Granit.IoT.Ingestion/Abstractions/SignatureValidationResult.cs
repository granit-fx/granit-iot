namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Outcome of <see cref="IPayloadSignatureValidator.ValidateAsync"/>.
/// </summary>
public sealed record SignatureValidationResult(bool IsValid, string? FailureReason = null)
{
    public static SignatureValidationResult Valid { get; } = new(IsValid: true);

    public static SignatureValidationResult Invalid(string reason) => new(IsValid: false, reason);
}
