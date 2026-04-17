using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.Credentials.Internal;

internal static partial class RotatingAwsIoTCredentialLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "AWS IoT credentials loaded from secret store (access key id '{accessKeyId}').")]
    public static partial void CredentialsLoaded(ILogger logger, string accessKeyId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "AWS IoT credential refresh failed; continuing with the previously cached credentials.")]
    public static partial void RefreshFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Initial AWS IoT credential fetch did not complete within {timeoutSeconds}s; provider is still not ready.")]
    public static partial void InitialFetchTimedOut(ILogger logger, int timeoutSeconds);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "AWS IoT credential rotation detected (new access key id '{accessKeyId}').")]
    public static partial void RotationDetected(ILogger logger, string accessKeyId);
}
