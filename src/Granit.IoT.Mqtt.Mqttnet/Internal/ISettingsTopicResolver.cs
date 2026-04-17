using Granit.IoT.Mqtt;
using Granit.Settings.Services;

namespace Granit.IoT.Mqtt.Mqttnet.Internal;

/// <summary>
/// Resolves the MQTT topic the bridge subscribes to, falling back to the default in
/// the <see cref="IoTMqttSettingNames.TopicPattern"/> setting definition when no
/// per-tenant override is set.
/// </summary>
internal interface ISettingsTopicResolver
{
    Task<string> ResolveAsync(CancellationToken cancellationToken);
}

internal sealed class SettingsTopicResolver(ISettingProvider settings) : ISettingsTopicResolver
{
    private const string DefaultTopicPattern = "devices/+/telemetry";

    public async Task<string> ResolveAsync(CancellationToken cancellationToken)
    {
        string? configured = await settings
            .GetOrNullAsync(IoTMqttSettingNames.TopicPattern, cancellationToken)
            .ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(configured) ? DefaultTopicPattern : configured;
    }
}
