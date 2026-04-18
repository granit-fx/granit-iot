using System.Text.Json;

namespace Granit.IoT.Ingestion;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used by every inbound ingestion
/// parser. Bounds the deserialization depth and forbids trailing commas /
/// comments so a crafted payload cannot stack-overflow the parser or sneak in
/// ambiguous syntax. Source-generated contexts (one per provider) are still
/// used for the hot path; these options govern ad-hoc calls.
/// </summary>
public static class IngestionJsonOptions
{
    /// <summary>
    /// Hard cap on JSON nesting. Telemetry envelopes are flat: no legitimate
    /// payload needs more than a handful of levels. Keeping this tight is the
    /// cheapest mitigation against a stack-overflow DoS.
    /// </summary>
    public const int MaxDepth = 8;

    /// <summary>
    /// Shared <see cref="JsonSerializerOptions"/> with a bounded depth and
    /// strict syntax rules. Safe to reuse across threads.
    /// </summary>
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = MaxDepth,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        PropertyNameCaseInsensitive = false,
    };
}
