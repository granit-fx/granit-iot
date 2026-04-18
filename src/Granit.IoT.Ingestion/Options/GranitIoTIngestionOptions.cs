using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Ingestion.Options;

/// <summary>
/// Options for the IoT ingestion pipeline. Bound from the <c>IoT:Ingestion</c>
/// configuration section.
/// </summary>
public sealed class GranitIoTIngestionOptions
{
    /// <summary>Configuration section name for binding via <c>BindConfiguration(SectionName)</c>.</summary>
    public const string SectionName = "IoT:Ingestion";

    /// <summary>
    /// TTL of the transport message id deduplication entry in Redis.
    /// Defaults to 5 minutes — matches typical IoT-hub retry windows.
    /// </summary>
    [Range(1, 60)]
    public int DeduplicationWindowMinutes { get; set; } = 5;
}
