using Granit.IoT.Domain;
using Shouldly;

namespace Granit.IoT.Tests.Domain;

public sealed class HardwareModelTests
{
    [Fact]
    public void Create_ValidModel_Succeeds()
    {
        var model = HardwareModel.Create("Sensor-V2");

        model.Value.ShouldBe("Sensor-V2");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_NullOrEmpty_Throws(string? value)
    {
        Should.Throw<ArgumentException>(() => HardwareModel.Create(value!));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        string tooLong = new('A', HardwareModel.MaxLength + 1);

        Should.Throw<ArgumentException>(() => HardwareModel.Create(tooLong));
    }
}
