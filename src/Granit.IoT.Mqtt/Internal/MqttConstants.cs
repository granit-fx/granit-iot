namespace Granit.IoT.Mqtt.Internal;

internal static class MqttConstants
{
    /// <summary>
    /// Source discriminator passed to <c>IIngestionPipeline.ProcessAsync</c>. One value
    /// covers all MQTT brokers — broker-specific differences live in the configurable
    /// parser/options, not in the discriminator.
    /// </summary>
    internal const string SourceName = "mqtt";

    /// <summary>Maximum length of the MQTT topic the bridge will accept (matches MQTT spec).</summary>
    internal const int MaxTopicLength = 65_535;

    /// <summary>Maximum number of '/' segments in a topic before it is rejected as malformed.</summary>
    internal const int MaxTopicSegments = 16;
}
