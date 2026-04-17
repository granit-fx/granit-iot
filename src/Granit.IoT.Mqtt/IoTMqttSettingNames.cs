namespace Granit.IoT.Mqtt;

/// <summary>
/// Setting keys consumed by the MQTT bridge. Auto-discovered by <c>Granit.Settings</c>
/// via the internal <c>IoTMqttSettingDefinitionProvider</c>.
/// </summary>
/// <remarks>
/// These constants hold the literal string keys used to look up values in
/// <c>ISettingProvider</c> — they are NOT secret material. Actual sensitive data
/// (the client certificate, the password) lives in <c>Granit.Vault</c> /
/// <c>Granit.Settings</c> at runtime and never appears in source.
/// </remarks>
#pragma warning disable GRSEC003 // Constant names point at vault keys, not secrets.
public static class IoTMqttSettingNames
{
    /// <summary>
    /// MQTT topic subscription pattern. Supports wildcards (<c>+</c> single segment,
    /// <c>#</c> multi-segment). Example: <c>devices/+/telemetry</c>.
    /// </summary>
    public const string TopicPattern = "IoT:Mqtt:TopicPattern";

    /// <summary>
    /// Vault secret name (NOT the certificate itself) holding the PFX or PEM bytes
    /// used for mTLS broker authentication. Resolved at startup via
    /// <c>ISecretStore.GetSecretAsync</c>.
    /// </summary>
    public const string CertificateSecretName = "IoT:Mqtt:CertificateSecretName";

    /// <summary>
    /// Optional password protecting the client certificate. Carries
    /// <c>[SensitiveData]</c> so logs and audit trails redact it.
    /// </summary>
    public const string CertificatePassword = "IoT:Mqtt:CertificatePassword";

    /// <summary>Default QoS level applied when none is specified per-topic. Range 0..2.</summary>
    public const string DefaultQoS = "IoT:Mqtt:DefaultQoS";

    /// <summary>Feature flag name gating the MQTT bridge per tenant.</summary>
    public const string FeatureFlag = "IoT.MqttBridge";
}
#pragma warning restore GRSEC003
