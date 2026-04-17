using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Mqtt.Internal;
using Granit.IoT.Mqtt.Options;
using Granit.Settings.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Mqtt.Extensions;

public static class IoTMqttServiceCollectionExtensions
{
    public static IServiceCollection AddGranitIoTMqtt(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<IoTMqttOptions>()
            .BindConfiguration(IoTMqttOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddSingleton<IInboundMessageParser, MqttMessageParser>();
        services.AddSingleton<IPayloadSignatureValidator, MqttPayloadSignatureValidator>();
        services.AddSingleton<ISettingDefinitionProvider, IoTMqttSettingDefinitionProvider>();

        // Default no-op bridge — replaced by Granit.IoT.Mqtt.Mqttnet via services.Replace.
        services.TryAddSingleton<IIoTMqttBridge, NullIoTMqttBridge>();

        return services;
    }
}
