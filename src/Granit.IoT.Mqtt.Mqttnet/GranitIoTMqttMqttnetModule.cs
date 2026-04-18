using Granit.IoT.Mqtt;
using Granit.IoT.Mqtt.Mqttnet.Extensions;
using Granit.Modularity;
using Granit.Vault;

namespace Granit.IoT.Mqtt.Mqttnet;

/// <summary>
/// MQTTnet v5 implementation module. Replaces the no-op <c>NullIoTMqttBridge</c>
/// registered by <c>GranitIoTMqttModule</c> with the live <c>MqttnetIoTBridge</c>
/// (also registered as an <see cref="Microsoft.Extensions.Hosting.IHostedService"/>
/// so the broker connection follows the host lifecycle).
/// </summary>
[DependsOn(typeof(GranitIoTMqttModule))]
[DependsOn(typeof(GranitVaultModule))]
public sealed class GranitIoTMqttMqttnetModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTMqttMqttnet();
    }
}
