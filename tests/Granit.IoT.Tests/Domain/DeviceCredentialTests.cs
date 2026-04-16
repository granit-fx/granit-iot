using Granit.IoT.Domain;
using Shouldly;

namespace Granit.IoT.Tests.Domain;

public sealed class DeviceCredentialTests
{
    [Fact]
    public void Create_ValidInput_Succeeds()
    {
        var credential = DeviceCredential.Create("hmac-sha256", "secret123");

        credential.CredentialType.ShouldBe("hmac-sha256");
        credential.ProtectedSecret.ShouldBe("secret123");
    }

    [Fact]
    public void Create_NullCredentialType_Throws()
    {
        Should.Throw<ArgumentException>(() => DeviceCredential.Create(null!, "secret"));
    }

    [Fact]
    public void Create_NullSecret_Throws()
    {
        Should.Throw<ArgumentException>(() => DeviceCredential.Create("hmac", null!));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = DeviceCredential.Create("hmac", "secret");
        var b = DeviceCredential.Create("hmac", "secret");

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = DeviceCredential.Create("hmac", "secret1");
        var b = DeviceCredential.Create("hmac", "secret2");

        a.ShouldNotBe(b);
    }
}
