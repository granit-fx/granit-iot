using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Mqtt.Options;

/// <summary>
/// Connection and runtime tuning for the MQTT bridge. Bound from the <c>IoT:Mqtt</c>
/// configuration section. Hot-reload supported via <c>IOptionsMonitor&lt;IoTMqttOptions&gt;</c>.
/// </summary>
public sealed class IoTMqttOptions
{
    public const string SectionName = "IoT:Mqtt";

    /// <summary>
    /// MQTT broker URI (e.g. <c>mqtts://broker.example.com:8883</c>). Only the
    /// <c>mqtts://</c> scheme is permitted in production — plaintext MQTT is rejected
    /// by the bridge at startup.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string BrokerUri { get; set; } = string.Empty;

    /// <summary>MQTT client identifier presented during the CONNECT packet.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ClientId { get; set; } = "granit-iot";

    /// <summary>Default QoS level (0..2) applied when not overridden per-topic.</summary>
    [Range(0, 2)]
    public int DefaultQoS { get; set; } = 1;

    /// <summary>
    /// Maximum payload size accepted from the broker. Mirrors the HTTP webhook limit
    /// (<see cref="MaxPayloadBytesDefault"/>). Larger payloads are dropped with a metric.
    /// </summary>
    [Range(1, 10 * 1024 * 1024)]
    public int MaxPayloadBytes { get; set; } = MaxPayloadBytesDefault;

    public const int MaxPayloadBytesDefault = 256 * 1024;

    /// <summary>MQTT keep-alive (seconds) sent in the CONNECT packet.</summary>
    [Range(5, 600)]
    public int KeepAliveSeconds { get; set; } = 60;

    /// <summary>
    /// Local snapshot cache TTL for the <c>IoT.MqttBridge</c> per-tenant feature flag.
    /// Removes the per-message <c>Task</c> allocation pressure that
    /// <c>IFeatureChecker.IsEnabledAsync</c> would otherwise add to the hot loop.
    /// Tenant flag flips become visible within this window.
    /// </summary>
    [Range(1, 300)]
    public int FeatureFlagCacheSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum messages buffered locally between the broker and the ingestion pipeline
    /// (back-pressure cap — drops when full).
    /// </summary>
    [Range(1, 100_000)]
    public int MaxPendingMessages { get; set; } = 1_000;

    /// <summary>
    /// How many minutes before the loaded certificate's <c>ExpiresOn</c> the bridge
    /// proactively re-fetches it from the vault and reconnects. Avoids relying on the
    /// MQTTnet client to detect mid-connection TLS expiry.
    /// </summary>
    [Range(1, 60 * 24)]
    public int CertificateExpiryWarningMinutes { get; set; } = 5;
}
