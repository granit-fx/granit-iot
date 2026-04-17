using Granit.IoT.Mqtt;
using Granit.IoT.Mqtt.Mqttnet.Internal;
using Granit.Settings.Services;
using Granit.Vault;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Mqtt.Mqttnet.Tests.Internal;

public sealed class SecretStoreCertificateLoaderTests
{
    [Fact]
    public async Task LoadAsync_MissingSecretName_Throws()
    {
        ISecretStore vault = Substitute.For<ISecretStore>();
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(IoTMqttSettingNames.CertificateSecretName, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        SecretStoreCertificateLoader loader = new(vault, settings);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await loader.LoadAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldContain(IoTMqttSettingNames.CertificateSecretName);
        await vault.DidNotReceive().GetSecretAsync(Arg.Any<SecretRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadAsync_SecretNotFound_PropagatesException()
    {
        ISecretStore vault = Substitute.For<ISecretStore>();
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(IoTMqttSettingNames.CertificateSecretName, Arg.Any<CancellationToken>())
            .Returns("granit/mqtt/cert");
        vault.GetSecretAsync(Arg.Any<SecretRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<SecretDescriptor>>(_ => throw new Granit.Vault.Exceptions.SecretNotFoundException("granit/mqtt/cert"));

        SecretStoreCertificateLoader loader = new(vault, settings);

        await Should.ThrowAsync<Granit.Vault.Exceptions.SecretNotFoundException>(async () =>
            await loader.LoadAsync(TestContext.Current.CancellationToken));
    }
}
