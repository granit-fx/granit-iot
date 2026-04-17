using Granit.IoT.Mqtt;
using Granit.IoT.Mqtt.Internal;
using Granit.Settings.Definitions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Mqtt.Tests.Settings;

public sealed class IoTMqttSettingDefinitionProviderTests
{
    [Fact]
    public void Define_RegistersAllFourSettings()
    {
        List<SettingDefinition> captured = CaptureDefinitions();

        captured.Count.ShouldBe(4);
        captured.Select(d => d.Name).ShouldBe(
            [
                IoTMqttSettingNames.TopicPattern,
                IoTMqttSettingNames.CertificateSecretName,
                IoTMqttSettingNames.CertificatePassword,
                IoTMqttSettingNames.DefaultQoS,
            ],
            ignoreOrder: true);

        foreach (SettingDefinition def in captured)
        {
            def.Providers.ShouldBe(["T", "G"], ignoreOrder: true);
        }
    }

    [Fact]
    public void Define_TopicPattern_HasDefaultAndIsClientVisible()
    {
        SettingDefinition def = CaptureDefinitions().Single(d => d.Name == IoTMqttSettingNames.TopicPattern);

        def.DefaultValue.ShouldBe("devices/+/telemetry");
        def.IsVisibleToClients.ShouldBeTrue();
    }

    [Fact]
    public void Define_CertificateSecretName_HasNoDefaultAndHidden()
    {
        SettingDefinition def = CaptureDefinitions().Single(d => d.Name == IoTMqttSettingNames.CertificateSecretName);

        def.DefaultValue.ShouldBeNull();
        def.IsVisibleToClients.ShouldBeFalse();
    }

    [Fact]
    public void Define_CertificatePassword_IsHidden()
    {
        // The setting itself isn't [SensitiveData] (the attribute applies on the in-memory
        // value, not the definition), but we guarantee it isn't exposed to API consumers.
        SettingDefinition def = CaptureDefinitions().Single(d => d.Name == IoTMqttSettingNames.CertificatePassword);

        def.IsVisibleToClients.ShouldBeFalse();
    }

    [Fact]
    public void Define_DefaultQoS_DefaultsToOne()
    {
        SettingDefinition def = CaptureDefinitions().Single(d => d.Name == IoTMqttSettingNames.DefaultQoS);

        def.DefaultValue.ShouldBe("1");
        def.IsVisibleToClients.ShouldBeTrue();
    }

    private static List<SettingDefinition> CaptureDefinitions()
    {
        List<SettingDefinition> captured = [];
        ISettingDefinitionContext context = Substitute.For<ISettingDefinitionContext>();
        context.When(c => c.Add(Arg.Any<SettingDefinition>()))
            .Do(call => captured.Add(call.Arg<SettingDefinition>()));

        new IoTMqttSettingDefinitionProvider().Define(context);
        return captured;
    }
}
