using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.Jobs.Internal;

internal static partial class JobsLog
{
    [LoggerMessage(EventId = 5001, Level = LogLevel.Information,
        Message = "AWS IoT Job '{jobId}' created (operation '{operation}').")]
    public static partial void JobCreated(ILogger logger, string jobId, string operation);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information,
        Message = "AWS IoT Job '{jobId}' already exists; treating dispatch as idempotent reuse.")]
    public static partial void JobAlreadyExists(ILogger logger, string jobId);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information,
        Message = "AWS IoT command {correlationId} reused tracked job '{jobId}'.")]
    public static partial void IdempotentReuse(ILogger logger, Guid correlationId, string jobId);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Warning,
        Message = "AWS IoT Job '{jobId}' dispatch failed.")]
    public static partial void DispatchFailed(ILogger logger, string jobId, Exception exception);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Information,
        Message = "AWS IoT dynamic ThingGroup '{groupName}' created for query '{query}'.")]
    public static partial void DynamicGroupCreated(ILogger logger, string groupName, string query);

    [LoggerMessage(EventId = 5006, Level = LogLevel.Information,
        Message = "AWS IoT Job '{jobId}' completed for Thing '{thingName}'.")]
    public static partial void JobCompleted(ILogger logger, string jobId, string thingName);

    [LoggerMessage(EventId = 5007, Level = LogLevel.Warning,
        Message = "AWS IoT Job '{jobId}' for Thing '{thingName}' ended with status '{status}': {reason}")]
    public static partial void JobFailed(ILogger logger, string jobId, string thingName, string status, string? reason);

    [LoggerMessage(EventId = 5008, Level = LogLevel.Warning,
        Message = "Job status polling tick failed for job '{jobId}'; will retry on next interval.")]
    public static partial void PollingTickFailed(ILogger logger, string jobId, Exception exception);
}
