using Granit.IoT.Notifications;
using Granit.IoT.Notifications.Internal;
using Granit.Settings.Definitions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Notifications.Tests.Settings;

public sealed class IoTSettingDefinitionProviderTests
{
    [Fact]
    public void Define_RegistersExpectedSettings()
    {
        List<SettingDefinition> captured = CaptureDefinitions();

        captured.Count.ShouldBe(4);
        captured.Select(d => d.Name).ShouldBe(
            [
                IoTSettingNames.TelemetryRetentionDays,
                IoTSettingNames.IngestRateLimit,
                IoTSettingNames.NotificationThrottleMinutes,
                "IoT:Threshold",
            ],
            ignoreOrder: true);
    }

    [Theory]
    [InlineData("IoT:TelemetryRetentionDays", "90")]
    [InlineData("IoT:IngestRateLimit", "1000")]
    [InlineData("IoT:NotificationThrottleMinutes", "15")]
    public void Define_KeySetting_HasExpectedDefault(string name, string expectedDefault)
    {
        SettingDefinition definition = CaptureDefinitions().Single(d => d.Name == name);

        definition.DefaultValue.ShouldBe(expectedDefault);
        definition.IsVisibleToClients.ShouldBeTrue();
        definition.Providers.ShouldBe(["T", "G"], ignoreOrder: true);
    }

    [Fact]
    public void Define_ThresholdPattern_IsHiddenFromClients()
    {
        SettingDefinition definition = CaptureDefinitions().Single(d => d.Name == "IoT:Threshold");

        definition.DefaultValue.ShouldBeNull();
        definition.IsVisibleToClients.ShouldBeFalse();
        definition.Providers.ShouldBe(["T", "G"], ignoreOrder: true);
    }

    private static List<SettingDefinition> CaptureDefinitions()
    {
        List<SettingDefinition> captured = [];
        ISettingDefinitionContext context = Substitute.For<ISettingDefinitionContext>();
        context.When(c => c.Add(Arg.Any<SettingDefinition>()))
            .Do(call => captured.Add(call.Arg<SettingDefinition>()));

        new IoTSettingDefinitionProvider().Define(context);
        return captured;
    }
}
