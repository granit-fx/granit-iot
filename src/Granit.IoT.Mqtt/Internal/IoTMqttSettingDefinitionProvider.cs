using Granit.Settings.Definitions;

namespace Granit.IoT.Mqtt.Internal;

/// <summary>
/// Registers per-tenant MQTT bridge settings (topic pattern, vault secret name for
/// the client certificate, certificate password, default QoS). Auto-discovered by
/// <c>GranitSettingsModule</c>.
/// </summary>
internal sealed class IoTMqttSettingDefinitionProvider : ISettingDefinitionProvider
{
    private const string TenantProvider = "T";
    private const string GlobalProvider = "G";

    public void Define(ISettingDefinitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Add(new SettingDefinition(IoTMqttSettingNames.TopicPattern)
        {
            DefaultValue = "devices/+/telemetry",
            IsVisibleToClients = true,
            DisplayName = "MQTT topic pattern",
            Description = "MQTT topic subscription pattern. '+' matches a single segment, '#' a multi-segment suffix. Default subscribes to telemetry from any device.",
            Providers = { TenantProvider, GlobalProvider },
        });

        context.Add(new SettingDefinition(IoTMqttSettingNames.CertificateSecretName)
        {
            DefaultValue = null,
            IsVisibleToClients = false,
            DisplayName = "Client certificate vault secret name",
            Description = "Vault key (NOT the certificate itself) under which the MQTT client certificate is stored. Resolved at startup via Granit.Vault's ISecretStore.",
            Providers = { TenantProvider, GlobalProvider },
        });

        context.Add(new SettingDefinition(IoTMqttSettingNames.CertificatePassword)
        {
            DefaultValue = null,
            IsVisibleToClients = false,
            DisplayName = "Client certificate password",
            Description = "Optional password protecting the PFX/PEM client certificate. Stored encrypted at rest and redacted from logs (sensitive).",
            Providers = { TenantProvider, GlobalProvider },
        });

        context.Add(new SettingDefinition(IoTMqttSettingNames.DefaultQoS)
        {
            DefaultValue = "1",
            IsVisibleToClients = true,
            DisplayName = "Default MQTT QoS",
            Description = "QoS level applied when a topic does not specify one. 0 = at-most-once, 1 = at-least-once, 2 = exactly-once.",
            Providers = { TenantProvider, GlobalProvider },
        });
    }
}
