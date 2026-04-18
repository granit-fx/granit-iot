using System.ComponentModel.DataAnnotations;
using Granit.IoT.Mqtt.Options;
using Shouldly;

namespace Granit.IoT.Mqtt.Tests.Options;

public sealed class IoTMqttOptionsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        IoTMqttOptions opts = new();

        opts.ClientId.ShouldBe("granit-iot");
        opts.DefaultQoS.ShouldBe(1);
        opts.MaxPayloadBytes.ShouldBe(IoTMqttOptions.MaxPayloadBytesDefault);
        opts.KeepAliveSeconds.ShouldBe(60);
        opts.FeatureFlagCacheSeconds.ShouldBe(30);
        opts.MaxPendingMessages.ShouldBe(1_000);
        opts.CertificateExpiryWarningMinutes.ShouldBe(5);
    }

    [Fact]
    public void SectionName_IsStable()
    {
        IoTMqttOptions.SectionName.ShouldBe("IoT:Mqtt");
    }

    [Fact]
    public void Validation_RequiresBrokerUriAndClientId()
    {
        IoTMqttOptions opts = new() { BrokerUri = "", ClientId = "" };
        List<ValidationResult> results = [];
        bool ok = Validator.TryValidateObject(opts, new ValidationContext(opts), results, validateAllProperties: true);

        ok.ShouldBeFalse();
        results.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void Validation_QoSRange(int qos, bool valid)
    {
        IoTMqttOptions opts = new() { BrokerUri = "mqtts://b", ClientId = "c", DefaultQoS = qos };
        List<ValidationResult> results = [];
        bool ok = Validator.TryValidateObject(opts, new ValidationContext(opts), results, validateAllProperties: true);
        ok.ShouldBe(valid);
    }
}
