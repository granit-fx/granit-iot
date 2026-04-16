using Granit.IoT.Domain;
using Shouldly;

namespace Granit.IoT.Tests.Domain;

public sealed class FirmwareVersionTests
{
    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0")]
    [InlineData("2.1.3")]
    [InlineData("1.0.0-beta")]
    [InlineData("1.0.0+build.123")]
    public void Create_ValidVersion_Succeeds(string value)
    {
        var version = FirmwareVersion.Create(value);

        version.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_NullOrEmpty_Throws(string? value)
    {
        Should.Throw<ArgumentException>(() => FirmwareVersion.Create(value!));
    }

    [Theory]
    [InlineData("notaversion")]
    [InlineData("1")]
    [InlineData("v1.0")]
    public void Create_InvalidFormat_Throws(string value)
    {
        Should.Throw<ArgumentException>(() => FirmwareVersion.Create(value));
    }
}
