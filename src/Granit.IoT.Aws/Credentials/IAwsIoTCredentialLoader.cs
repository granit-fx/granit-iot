namespace Granit.IoT.Aws.Credentials;

/// <summary>
/// Fetches a fresh credential pair from a secret store. Plugged into the
/// rotating credential provider so that <c>Granit.IoT.Aws</c> stays free of
/// any AWS SDK reference: the actual <c>IAmazonSecretsManager</c> call lives
/// in the companion package shipped with PR #4 / story #47.
/// </summary>
public interface IAwsIoTCredentialLoader
{
    /// <summary>
    /// Returns the latest credential value persisted under
    /// <see cref="AwsIoTCredentialOptions.FleetCredentialSecretArn"/>, or
    /// <c>null</c> if the configured ARN intentionally signals
    /// "use the IAM role / default chain". Implementations should keep their
    /// own retry policy — the rotating provider treats any thrown exception
    /// as transient and keeps the previous value (stale-ok semantics).
    /// </summary>
    Task<LoadedAwsIoTCredentials?> LoadAsync(CancellationToken cancellationToken);
}
