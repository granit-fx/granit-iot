using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Ingestion.Scaleway.Options;

/// <summary>
/// Options for the Scaleway IoT Hub ingestion provider. Bound from the
/// <c>IoT:Ingestion:Scaleway</c> configuration section. Hot-reload supported via
/// <c>IOptionsMonitor&lt;ScalewayIoTOptions&gt;</c>.
/// </summary>
public sealed class ScalewayIoTOptions
{
    /// <summary>Configuration section name for binding via <c>BindConfiguration(SectionName)</c>.</summary>
    public const string SectionName = "IoT:Ingestion:Scaleway";

    /// <summary>
    /// Shared secret configured in the Scaleway IoT Hub console. Used to verify the
    /// HMAC-SHA256 signature carried in the <c>X-Scaleway-Signature</c> header.
    /// </summary>
    /// <remarks>
    /// Must be empty in non-<c>Development</c> environments (<c>ScalewayIoTOptionsValidator</c>
    /// enforces this). Production deployments should resolve this value from
    /// <c>Granit.Vault</c> at startup (or via the <c>IOptionsMonitor</c> reload path)
    /// so the secret never lands in an appsettings file or environment variable.
    /// The signature validator also fail-closes when the value is missing — a rejected
    /// Scaleway feed surfaces as a 401 rather than silently accepting traffic.
    /// </remarks>
    public string SharedSecret { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based index of the topic segment that holds the device serial number.
    /// Defaults to <c>1</c> — i.e. <c>devices/{serial}/...</c>.
    /// </summary>
    [Range(0, 16)]
    public int TopicDeviceSegmentIndex { get; set; } = 1;
}
