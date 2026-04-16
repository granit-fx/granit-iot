using Granit.IoT.Domain;
using Shouldly;

namespace Granit.IoT.Tests.Domain;

public sealed class MetricNameTests
{
    [Theory]
    [InlineData("temperature")]
    [InlineData("sensor.battery")]
    [InlineData("sensor.battery.level")]
    [InlineData("a")]
    public void Create_ValidName_Succeeds(string value)
    {
        var name = MetricName.Create(value);

        name.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_NullOrEmpty_Throws(string? value)
    {
        Should.Throw<ArgumentException>(() => MetricName.Create(value!));
    }

    [Theory]
    [InlineData("Temperature")]
    [InlineData("UPPER")]
    [InlineData("has spaces")]
    [InlineData(".starts.with.dot")]
    [InlineData("ends.with.")]
    [InlineData("double..dot")]
    [InlineData("1startswithnumber")]
    public void Create_InvalidFormat_Throws(string value)
    {
        Should.Throw<ArgumentException>(() => MetricName.Create(value));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        string tooLong = new('a', MetricName.MaxLength + 1);

        Should.Throw<ArgumentException>(() => MetricName.Create(tooLong));
    }
}
