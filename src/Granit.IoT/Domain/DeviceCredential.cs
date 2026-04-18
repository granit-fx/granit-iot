using Granit.DataProtection;
using Granit.Domain;

namespace Granit.IoT.Domain;

/// <summary>
/// Value object representing a device's authentication material. The credential type
/// is a free-form discriminator (e.g. <c>"api-key"</c>, <c>"x509-thumbprint"</c>,
/// <c>"aws-iot-cert-arn"</c>); the secret is tagged <see cref="Sensitivity.Restricted"/>
/// and is encrypted at rest by the Granit data-protection pipeline.
/// </summary>
public sealed class DeviceCredential : ValueObject
{
    /// <summary>Maximum length (in characters) allowed for <see cref="CredentialType"/>.</summary>
    public const int MaxCredentialTypeLength = 64;

    /// <summary>Maximum length (in characters) allowed for <see cref="ProtectedSecret"/> ciphertext.</summary>
    public const int MaxProtectedSecretLength = 2048;

    /// <summary>Free-form discriminator identifying the credential scheme (e.g. <c>"api-key"</c>, <c>"x509-thumbprint"</c>).</summary>
    public string CredentialType { get; init; } = string.Empty;

    /// <summary>Encrypted secret material. Omitted from logs and serialization by the Granit data-protection attribute.</summary>
    [SensitiveData(Level = Sensitivity.Restricted, Mode = SensitiveDataMode.Omit)]
    public string ProtectedSecret { get; init; } = string.Empty;

    /// <summary>
    /// Factory method — the only supported construction path. Validates that both
    /// fields are non-empty; does not validate length (enforced by EF configuration).
    /// </summary>
    /// <param name="credentialType">Credential scheme discriminator. Required.</param>
    /// <param name="protectedSecret">Already-encrypted secret value. Required.</param>
    /// <exception cref="ArgumentException">Thrown when either argument is null, empty, or whitespace.</exception>
    public static DeviceCredential Create(string credentialType, string protectedSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialType);
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedSecret);
        return new DeviceCredential { CredentialType = credentialType, ProtectedSecret = protectedSecret };
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CredentialType;
        yield return ProtectedSecret;
    }
}
