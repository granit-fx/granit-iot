using Granit.IoT.Ingestion;
using Granit.IoT.Mqtt.Internal;
using Shouldly;

namespace Granit.IoT.Mqtt.Tests.Parsing;

public sealed class MqttTopicMapperTests
{
    [Theory]
    [InlineData("devices/SN-001/telemetry", "SN-001")]
    [InlineData("devices/abc123/data/temperature", "abc123")]
    public void ExtractDeviceSerial_ExtractsSecondSegment(string topic, string expected) =>
        MqttTopicMapper.ExtractDeviceSerial(topic).ShouldBe(expected);

    [Theory]
    [InlineData("devices")]                     // Missing serial segment
    [InlineData("not-devices/SN-001/telemetry")] // Wrong root
    [InlineData("devices/SN 001/telemetry")]    // Invalid character (space)
    public void ExtractDeviceSerial_RejectsMalformedTopic(string topic) =>
        Should.Throw<IngestionParseException>(() => MqttTopicMapper.ExtractDeviceSerial(topic));

    [Fact]
    public void ExtractDeviceSerial_RejectsTopicLongerThanLimit()
    {
        string huge = "devices/" + new string('a', 65_600);
        Should.Throw<IngestionParseException>(() => MqttTopicMapper.ExtractDeviceSerial(huge));
    }
}
