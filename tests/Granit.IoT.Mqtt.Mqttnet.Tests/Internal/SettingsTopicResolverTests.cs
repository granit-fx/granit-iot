using Granit.IoT.Mqtt;
using Granit.IoT.Mqtt.Mqttnet.Internal;
using Granit.Settings.Services;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Mqtt.Mqttnet.Tests.Internal;

public sealed class SettingsTopicResolverTests
{
    [Fact]
    public async Task ResolveAsync_NoOverride_ReturnsDefault()
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(IoTMqttSettingNames.TopicPattern, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        string topic = await new SettingsTopicResolver(settings).ResolveAsync(TestContext.Current.CancellationToken);

        topic.ShouldBe("devices/+/telemetry");
    }

    [Fact]
    public async Task ResolveAsync_Override_ReturnsConfiguredValue()
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(IoTMqttSettingNames.TopicPattern, Arg.Any<CancellationToken>())
            .Returns("tenant-a/devices/+/data");

        string topic = await new SettingsTopicResolver(settings).ResolveAsync(TestContext.Current.CancellationToken);

        topic.ShouldBe("tenant-a/devices/+/data");
    }

    [Fact]
    public async Task ResolveAsync_BlankOverride_ReturnsDefault()
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(IoTMqttSettingNames.TopicPattern, Arg.Any<CancellationToken>())
            .Returns("   ");

        string topic = await new SettingsTopicResolver(settings).ResolveAsync(TestContext.Current.CancellationToken);

        topic.ShouldBe("devices/+/telemetry");
    }
}
