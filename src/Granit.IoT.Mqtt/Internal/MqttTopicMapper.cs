using System.Text.RegularExpressions;
using Granit.IoT.Ingestion;

namespace Granit.IoT.Mqtt.Internal;

/// <summary>
/// Extracts the device serial number from an MQTT topic according to the convention
/// <c>devices/{serial}/...</c>. The serial is always the segment immediately following
/// the literal <c>devices</c> root — broker-specific deviations should publish to a
/// topic that respects this layout.
/// </summary>
internal static partial class MqttTopicMapper
{
    private const string DevicesRoot = "devices";
    private const int SerialSegmentIndex = 1;

    public static string ExtractDeviceSerial(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        if (topic.Length > MqttConstants.MaxTopicLength)
        {
            throw new IngestionParseException(
                $"MQTT topic exceeds {MqttConstants.MaxTopicLength} characters.");
        }

        if (!TopicPattern().IsMatch(topic))
        {
            throw new IngestionParseException($"Topic '{topic}' is not a valid MQTT topic pattern.");
        }

        string[] segments = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > MqttConstants.MaxTopicSegments)
        {
            throw new IngestionParseException(
                $"MQTT topic '{topic}' has more than {MqttConstants.MaxTopicSegments} segments.");
        }

        if (segments.Length <= SerialSegmentIndex
            || !segments[0].Equals(DevicesRoot, StringComparison.Ordinal))
        {
            throw new IngestionParseException(
                $"Topic '{topic}' does not follow the 'devices/{{serial}}/...' convention.");
        }

        return segments[SerialSegmentIndex];
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-]+(\/[A-Za-z0-9_\-]+){0,15}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TopicPattern();
}
