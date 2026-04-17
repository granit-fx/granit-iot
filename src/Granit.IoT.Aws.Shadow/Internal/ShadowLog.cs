using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.Shadow.Internal;

internal static partial class ShadowLog
{
    [LoggerMessage(EventId = 4901, Level = LogLevel.Debug,
        Message = "AWS IoT shadow reported state pushed for Thing '{thingName}'.")]
    public static partial void ReportedPushed(ILogger logger, string thingName);

    [LoggerMessage(EventId = 4902, Level = LogLevel.Warning,
        Message = "AWS IoT shadow update failed for Thing '{thingName}'.")]
    public static partial void ReportedPushFailed(ILogger logger, string thingName, Exception exception);

    [LoggerMessage(EventId = 4903, Level = LogLevel.Information,
        Message = "AWS IoT shadow delta detected for Thing '{thingName}' (version {version}, keys: {keys}).")]
    public static partial void DeltaDetected(ILogger logger, string thingName, long version, string keys);

    [LoggerMessage(EventId = 4904, Level = LogLevel.Warning,
        Message = "AWS IoT shadow polling tick failed for Thing '{thingName}'; will retry on next interval.")]
    public static partial void PollingTickFailed(ILogger logger, string thingName, Exception exception);
}
