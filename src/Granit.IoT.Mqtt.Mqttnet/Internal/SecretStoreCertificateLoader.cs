using System.Security.Cryptography.X509Certificates;
using Granit.IoT.Mqtt;
using Granit.Settings.Services;
using Granit.Vault;

namespace Granit.IoT.Mqtt.Mqttnet.Internal;

/// <summary>
/// Loads the MQTT client certificate from <c>Granit.Vault</c> via
/// <see cref="ISecretStore"/>. The vault key name is read from
/// <see cref="IoTMqttSettingNames.CertificateSecretName"/>; the optional PFX/PEM password
/// from <see cref="IoTMqttSettingNames.CertificatePassword"/>.
/// </summary>
internal sealed class SecretStoreCertificateLoader(
    ISecretStore secretStore,
    ISettingProvider settings) : ICertificateLoader
{
    public async Task<LoadedCertificate> LoadAsync(CancellationToken cancellationToken)
    {
        string? secretName = await settings
            .GetOrNullAsync(IoTMqttSettingNames.CertificateSecretName, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException(
                $"Setting '{IoTMqttSettingNames.CertificateSecretName}' is not configured — the MQTT bridge cannot establish mTLS.");
        }

        SecretDescriptor descriptor = await secretStore
            .GetSecretAsync(SecretRequest.Latest(secretName), cancellationToken)
            .ConfigureAwait(false);

        string? password = await settings
            .GetOrNullAsync(IoTMqttSettingNames.CertificatePassword, cancellationToken)
            .ConfigureAwait(false);

        X509Certificate2 cert = LoadFromBytes(descriptor.AsBytes(), password);
        DateTimeOffset expiresOn = descriptor.ExpiresOn ?? cert.NotAfter;

        return new LoadedCertificate(cert, expiresOn);
    }

    private static X509Certificate2 LoadFromBytes(byte[] bytes, string? password)
    {
        // PKCS#12 path covers both PFX and PEM-with-key-bundle producers most operators
        // wire up. If the secret turns out to be a PEM blob, X509CertificateLoader has a
        // dedicated overload — fall through to it.
        try
        {
            return X509CertificateLoader.LoadPkcs12(bytes, password);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return X509CertificateLoader.LoadCertificate(bytes);
        }
    }
}
