using Granit.IoT.Domain;
using Shouldly;

namespace Granit.IoT.Tests.Domain;

public sealed class DeviceSerialNumberTests
{
    [Theory]
    [InlineData("SN-001")]
    [InlineData("ABC123")]
    [InlineData("A")]
    [InlineData("Device_Serial-Number_123")]
    public void Create_ValidSerialNumber_Succeeds(string value)
    {
        var sn = DeviceSerialNumber.Create(value);

        sn.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_NullOrEmpty_Throws(string? value)
    {
        Should.Throw<ArgumentException>(() => DeviceSerialNumber.Create(value!));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        string tooLong = new('A', DeviceSerialNumber.MaxLength + 1);

        Should.Throw<ArgumentException>(() => DeviceSerialNumber.Create(tooLong));
    }

    [Theory]
    [InlineData("has spaces")]
    [InlineData("-starts-with-dash")]
    [InlineData("special!chars")]
    public void Create_InvalidFormat_Throws(string value)
    {
        Should.Throw<ArgumentException>(() => DeviceSerialNumber.Create(value));
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        var sn = DeviceSerialNumber.Create("SN-001");

        string value = sn;

        value.ShouldBe("SN-001");
    }

    [Fact]
    public void ImplicitConversion_FromString()
    {
        DeviceSerialNumber sn = "SN-002";

        sn.Value.ShouldBe("SN-002");
    }
}
