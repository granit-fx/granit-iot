namespace Granit.IoT.Mqtt.Mqttnet.Internal;

/// <summary>
/// Loads the mTLS client certificate the MQTTnet bridge presents to the broker.
/// The default <see cref="SecretStoreCertificateLoader"/> reads from
/// <c>Granit.Vault</c>'s <c>ISecretStore</c>; alternative implementations can be
/// substituted for tests or air-gapped deployments without touching the bridge.
/// </summary>
internal interface ICertificateLoader
{
    Task<LoadedCertificate> LoadAsync(CancellationToken cancellationToken);
}
