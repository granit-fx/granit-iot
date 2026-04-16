using System.Text.RegularExpressions;
using Granit.IoT.Ingestion.Scaleway.Options;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Scaleway.Internal;

/// <summary>
/// Extracts the device serial number from a Scaleway IoT Hub MQTT topic according to the
/// configured <see cref="ScalewayIoTOptions.TopicDeviceSegmentIndex"/> position.
/// </summary>
internal sealed partial class ScalewayTopicMapper(IOptionsMonitor<ScalewayIoTOptions> options)
{
    public string ExtractDeviceSerial(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        if (!TopicPattern().IsMatch(topic))
        {
            throw new IngestionParseException($"Topic '{topic}' is not a valid MQTT topic pattern.");
        }

        string[] segments = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int index = options.CurrentValue.TopicDeviceSegmentIndex;
        if (index >= segments.Length)
        {
            throw new IngestionParseException(
                $"Topic '{topic}' has fewer than {index + 1} segments — cannot extract device serial.");
        }

        return segments[index];
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-]+(\/[A-Za-z0-9_\-]+){0,9}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TopicPattern();
}
