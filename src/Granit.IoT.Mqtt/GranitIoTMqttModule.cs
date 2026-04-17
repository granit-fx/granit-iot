using Granit.Features;
using Granit.IoT.Ingestion;
using Granit.IoT.Mqtt.Extensions;
using Granit.Modularity;
using Granit.Settings;

namespace Granit.IoT.Mqtt;

/// <summary>
/// MQTT transport adapter for the IoT ingestion pipeline. Registers the MQTT message
/// parser, the mTLS-backed signature validator, the per-tenant setting definitions and
/// a no-op <see cref="IIoTMqttBridge"/>. Add the <c>Granit.IoT.Mqtt.Mqttnet</c> package
/// to plug in the MQTTnet v5 implementation.
/// </summary>
[DependsOn(typeof(GranitIoTIngestionModule))]
[DependsOn(typeof(GranitFeaturesModule))]
[DependsOn(typeof(GranitSettingsModule))]
public sealed class GranitIoTMqttModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTMqtt();
    }
}
