using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Ingestion.Scaleway.Options;

/// <summary>
/// Options for the Scaleway IoT Hub ingestion provider. Bound from the
/// <c>IoT:Ingestion:Scaleway</c> configuration section. Hot-reload supported via
/// <c>IOptionsMonitor&lt;ScalewayIoTOptions&gt;</c>.
/// </summary>
public sealed class ScalewayIoTOptions
{
    public const string SectionName = "IoT:Ingestion:Scaleway";

    /// <summary>
    /// Shared secret configured in the Scaleway IoT Hub console. Used to verify the
    /// HMAC-SHA256 signature carried in the <c>X-Scaleway-Signature</c> header.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string SharedSecret { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based index of the topic segment that holds the device serial number.
    /// Defaults to <c>1</c> — i.e. <c>devices/{serial}/...</c>.
    /// </summary>
    [Range(0, 16)]
    public int TopicDeviceSegmentIndex { get; set; } = 1;
}
