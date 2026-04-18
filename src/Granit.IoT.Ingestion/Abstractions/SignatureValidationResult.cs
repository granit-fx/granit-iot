namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Outcome of <see cref="IPayloadSignatureValidator.ValidateAsync"/>.
/// </summary>
/// <param name="IsValid"><c>true</c> when the provider signature verifies.</param>
/// <param name="FailureReason">Populated when <see cref="IsValid"/> is <c>false</c>; surfaced in the 401 response body.</param>
public sealed record SignatureValidationResult(bool IsValid, string? FailureReason = null)
{
    /// <summary>Singleton result for the valid-signature path.</summary>
    public static SignatureValidationResult Valid { get; } = new(IsValid: true);

    /// <summary>Factory for invalid-signature outcomes with the given reason.</summary>
    public static SignatureValidationResult Invalid(string reason) => new(IsValid: false, reason);
}
