namespace Granit.IoT.Ingestion.Aws;

/// <summary>
/// Supplies the secret access key used to derive a SigV4 signing key for a
/// given <see cref="ISigV4RequestValidator"/>. Implementations typically read
/// from <c>Granit.Vault</c> and rotate without restart.
/// </summary>
public interface ISigV4SigningKeyProvider
{
    /// <summary>
    /// Returns the secret access key bound to <paramref name="accessKeyId"/>,
    /// or <c>null</c> if the access key is unknown (which must be treated as
    /// an authentication failure by the caller).
    /// </summary>
    ValueTask<string?> GetSecretAccessKeyAsync(string accessKeyId, CancellationToken cancellationToken = default);
}
