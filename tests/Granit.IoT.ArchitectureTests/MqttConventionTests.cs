using ArchUnitNET.Domain;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Validates conventions specific to the MQTT bridge: the abstraction package must not
/// depend on the MQTTnet implementation, MQTT-specific parsers/validators must be
/// internal, and the MQTTnet bridge must implement both
/// <c>IIoTMqttBridge</c> and <c>IHostedService</c>.
/// </summary>
public sealed class MqttConventionTests
{
    private const string MqttAbstractionPrefix = "Granit.IoT.Mqtt";
    private const string MqttImplPrefix = "Granit.IoT.Mqtt.Mqttnet";
    private const string SignatureValidatorInterface = "Granit.IoT.Ingestion.Abstractions.IPayloadSignatureValidator";
    private const string MessageParserInterface = "Granit.IoT.Ingestion.Abstractions.IInboundMessageParser";
    private const string BridgeInterface = "Granit.IoT.Mqtt.IIoTMqttBridge";
    private const string HostedServiceInterface = "Microsoft.Extensions.Hosting.IHostedService";

    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void Mqtt_abstraction_must_not_depend_on_Mqttnet_implementation()
    {
        Class[] abstractionClasses = Architecture.Classes
            .Where(c => c.FullName.StartsWith(MqttAbstractionPrefix + ".", StringComparison.Ordinal)
                && !c.FullName.StartsWith(MqttImplPrefix + ".", StringComparison.Ordinal))
            .ToArray();

        IEnumerable<Class> violators = abstractionClasses
            .Where(c => c.Dependencies.Any(d => d.Target.FullName.StartsWith(MqttImplPrefix + ".", StringComparison.Ordinal)));

        violators.ShouldBeEmpty(
            "Granit.IoT.Mqtt must not depend on Granit.IoT.Mqtt.Mqttnet — the abstraction/implementation split is the entire point of the package separation. " +
            $"Violators: {string.Join(", ", violators.Select(c => c.FullName))}");
    }

    [Fact]
    public void Mqtt_message_parser_must_be_internal_and_sealed()
    {
        IEnumerable<Class> parsers = Architecture.Classes
            .Where(c => c.FullName.StartsWith(MqttAbstractionPrefix + ".", StringComparison.Ordinal))
            .Where(c => c.ImplementedInterfaces.Any(i => i.FullName == MessageParserInterface));

        parsers.ShouldNotBeEmpty();
        parsers.ShouldAllBe(c => c.Visibility != Visibility.Public);
        parsers.ShouldAllBe(c => c.IsSealed == true);
    }

    [Fact]
    public void Mqtt_signature_validator_must_be_internal_and_sealed()
    {
        IEnumerable<Class> validators = Architecture.Classes
            .Where(c => c.FullName.StartsWith(MqttAbstractionPrefix + ".", StringComparison.Ordinal))
            .Where(c => c.ImplementedInterfaces.Any(i => i.FullName == SignatureValidatorInterface));

        validators.ShouldNotBeEmpty();
        validators.ShouldAllBe(c => c.Visibility != Visibility.Public);
        validators.ShouldAllBe(c => c.IsSealed == true);
    }

    [Fact]
    public void MqttnetIoTBridge_must_implement_both_IIoTMqttBridge_and_IHostedService()
    {
        Class? bridge = Architecture.Classes
            .FirstOrDefault(c => c.FullName == "Granit.IoT.Mqtt.Mqttnet.Internal.MqttnetIoTBridge");

        bridge.ShouldNotBeNull("MqttnetIoTBridge must exist in Granit.IoT.Mqtt.Mqttnet.Internal.");
        bridge.ImplementedInterfaces.ShouldContain(i => i.FullName == BridgeInterface);
        bridge.ImplementedInterfaces.ShouldContain(i => i.FullName == HostedServiceInterface);
    }

    [Fact]
    public void IIoTMqttBridge_implementations_should_end_with_Bridge()
    {
        IEnumerable<Class> impls = Architecture.Classes
            .Where(c => c.FullName.StartsWith(MqttAbstractionPrefix, StringComparison.Ordinal))
            .Where(c => c.ImplementedInterfaces.Any(i => i.FullName == BridgeInterface));

        impls.ShouldNotBeEmpty();
        impls.ShouldAllBe(c => c.Name.EndsWith("Bridge", StringComparison.Ordinal));
    }
}
