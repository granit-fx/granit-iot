using Granit.DataProtection;
using Granit.Domain;

namespace Granit.IoT.Domain;

public sealed class DeviceCredential : ValueObject
{
    public const int MaxCredentialTypeLength = 64;
    public const int MaxProtectedSecretLength = 2048;

    public string CredentialType { get; init; } = string.Empty;

    [SensitiveData(Level = Sensitivity.Restricted, Mode = SensitiveDataMode.Omit)]
    public string ProtectedSecret { get; init; } = string.Empty;

    public static DeviceCredential Create(string credentialType, string protectedSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialType);
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedSecret);
        return new DeviceCredential { CredentialType = credentialType, ProtectedSecret = protectedSecret };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CredentialType;
        yield return ProtectedSecret;
    }
}
