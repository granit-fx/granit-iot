using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Granit.IoT.Mqtt.Mqttnet.Internal;
using Shouldly;

namespace Granit.IoT.Mqtt.Mqttnet.Tests.Internal;

public sealed class LoadedCertificateTests
{
    [Fact]
    public void Constructor_StoresCertificateAndExpiresOn()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        var expiresOn = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

        LoadedCertificate loaded = new(cert, expiresOn);

        loaded.Certificate.ShouldBeSameAs(cert);
        loaded.ExpiresOn.ShouldBe(expiresOn);
    }

    [Fact]
    public void Constructor_NullExpiresOn_Allowed()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        LoadedCertificate loaded = new(cert, null);

        loaded.ExpiresOn.ShouldBeNull();
    }
}
