using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Mqtt.Internal;
using Granit.IoT.Mqtt.Options;
using Granit.Settings.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Mqtt.Extensions;

/// <summary>
/// Service-collection extensions for the MQTT transport adapter
/// (<c>Granit.IoT.Mqtt</c>).
/// </summary>
public static class IoTMqttServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MQTT message parser, the mTLS-backed signature
    /// validator, the per-tenant setting definitions and a no-op
    /// <c>IIoTMqttBridge</c> (replaced by the MQTTnet module when
    /// <c>AddGranitIoTMqttMqttnet</c> is called).
    /// </summary>
    public static IServiceCollection AddGranitIoTMqtt(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<IoTMqttOptions>()
            .BindConfiguration(IoTMqttOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IInboundMessageParser, MqttMessageParser>();
        services.AddSingleton<IPayloadSignatureValidator, MqttPayloadSignatureValidator>();
        services.AddSingleton<ISettingDefinitionProvider, IoTMqttSettingDefinitionProvider>();

        // Default no-op bridge — replaced by Granit.IoT.Mqtt.Mqttnet via services.Replace.
        services.TryAddSingleton<IIoTMqttBridge, NullIoTMqttBridge>();

        return services;
    }
}
