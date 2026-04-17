using Granit.IoT.Aws.Domain;
using Shouldly;

namespace Granit.IoT.Aws.Tests.Domain;

public sealed class ThingNameTests
{
    private static readonly Guid SampleTenant = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void From_ProducesExpectedFormat()
    {
        var name = ThingName.From(SampleTenant, "SN-001");

        name.Value.ShouldBe("t11111111222233334444555555555555-SN-001");
    }

    [Fact]
    public void From_RoundTripsTenantAndSerial()
    {
        var name = ThingName.From(SampleTenant, "DeviceA_42");

        name.GetTenantId().ShouldBe(SampleTenant);
        name.GetSerialNumber().ShouldBe("DeviceA_42");
    }

    [Theory]
    [InlineData("SN-001")]
    [InlineData("Device_42")]
    [InlineData("ABC")]
    public void From_AcceptsValidSerial(string serial)
    {
        var name = ThingName.From(SampleTenant, serial);

        name.Value.Length.ShouldBeGreaterThan(33);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void From_RejectsEmptySerial(string? serial)
    {
        Should.Throw<ArgumentException>(() => ThingName.From(SampleTenant, serial!));
    }

    [Theory]
    [InlineData("contains spaces")]
    [InlineData("with!special")]
    [InlineData("-leadingdash")]
    public void From_RejectsInvalidSerial(string serial)
    {
        Should.Throw<ArgumentException>(() => ThingName.From(SampleTenant, serial));
    }

    [Fact]
    public void Create_AcceptsCanonicalValue()
    {
        const string Value = "t11111111222233334444555555555555-SN-001";

        var name = ThingName.Create(Value);

        name.Value.ShouldBe(Value);
    }

    [Theory]
    [InlineData("11111111222233334444555555555555-SN-001")]   // missing 't' prefix
    [InlineData("tABCD-SN-001")]                              // tenant id too short
    [InlineData("t1111111122223333444455555555555G-SN-001")]  // invalid hex char
    [InlineData("t11111111222233334444555555555555_SN-001")]  // missing dash separator
    public void Create_RejectsMalformedValue(string value)
    {
        Should.Throw<ArgumentException>(() => ThingName.Create(value));
    }

    [Fact]
    public void Create_RejectsValueExceedingMaxLength()
    {
        string serial = new('A', 100);
        string value = $"t{Guid.NewGuid():N}-{serial}";

        Should.Throw<ArgumentException>(() => ThingName.Create(value));
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        var name = ThingName.From(SampleTenant, "SN-001");

        string raw = name;

        raw.ShouldBe(name.Value);
    }
}
