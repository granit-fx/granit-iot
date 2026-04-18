using Granit.IoT.Mqtt;
using Granit.IoT.Mqtt.Mqttnet.Internal;
using Granit.IoT.Mqtt.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Mqtt.Mqttnet.Extensions;

/// <summary>
/// Service-collection extensions for the MQTTnet-backed MQTT bridge
/// (<c>Granit.IoT.Mqtt.Mqttnet</c>).
/// </summary>
public static class IoTMqttMqttnetServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the no-op <c>IIoTMqttBridge</c> with the live
    /// <c>MqttnetIoTBridge</c>, registers the certificate loader, the
    /// topic resolver, the feature-flag snapshot cache and the hosted
    /// service that follows the host lifecycle.
    /// </summary>
    public static IServiceCollection AddGranitIoTMqttMqttnet(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IoTMqttMetrics>();
        services.TryAddSingleton<ICertificateLoader, SecretStoreCertificateLoader>();
        services.TryAddSingleton<ISettingsTopicResolver, SettingsTopicResolver>();
        services.TryAddSingleton<FeatureFlagSnapshot>(sp =>
        {
            IOptionsMonitor<IoTMqttOptions> options =
                sp.GetRequiredService<IOptionsMonitor<IoTMqttOptions>>();
            return new FeatureFlagSnapshot(
                sp.GetRequiredService<Granit.Features.IFeatureChecker>(),
                sp.GetRequiredService<TimeProvider>(),
                TimeSpan.FromSeconds(options.CurrentValue.FeatureFlagCacheSeconds),
                IoTMqttSettingNames.FeatureFlag);
        });

        services.TryAddSingleton<MqttnetIoTBridge>();
        services.Replace(ServiceDescriptor.Singleton<IIoTMqttBridge>(
            sp => sp.GetRequiredService<MqttnetIoTBridge>()));
        services.AddHostedService(sp => sp.GetRequiredService<MqttnetIoTBridge>());

        return services;
    }
}
