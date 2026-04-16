using System.Globalization;
using Granit.Caching;
using Granit.IoT.Notifications.Abstractions;
using Granit.Settings.Services;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Notifications.Internal;

/// <summary>
/// Per-(device, metric) debounce so threshold notifications do not flood when a value
/// oscillates around the configured limit. Backed by <see cref="IConditionalCache"/>
/// (Redis SET-NX-PX in production via <c>Granit.Caching.StackExchangeRedis</c>,
/// in-memory in development).
/// </summary>
/// <remarks>
/// The throttle window is read per call from <c>IoT:NotificationThrottleMinutes</c>
/// via <see cref="ISettingProvider"/> so a tenant override takes effect immediately.
/// Cache failures fail open: the alert is published anyway, mirroring the policy
/// of <c>IdempotencyStoreInboundMessageDeduplicator</c>.
/// </remarks>
internal sealed partial class AlertThrottle(
    IConditionalCache cache,
    ISettingProvider settingProvider,
    ILogger<AlertThrottle> logger) : IAlertThrottle
{
    private const string KeyPrefix = "iot-alert:throttle:";
    private const int DefaultThrottleMinutes = 15;
    private const int MinThrottleMinutes = 1;
    private const int MaxThrottleMinutes = 1440;

    /// <summary>
    /// Returns <see langword="true"/> when no recent alert was published for the
    /// <c>(deviceId, metricName)</c> pair (caller should publish the notification),
    /// and <see langword="false"/> when an alert is already in the throttle window.
    /// </summary>
    public async Task<bool> TryAcquireAsync(
        Guid deviceId,
        string metricName,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName);

        TimeSpan window = await ResolveThrottleWindowAsync(cancellationToken).ConfigureAwait(false);
        string key = string.Concat(KeyPrefix, deviceId.ToString("N"), ":", metricName);

        try
        {
            return await cache
                .SetIfAbsentAsync(key, value: 1, window, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogThrottleCacheUnavailable(logger, ex, key, tenantId);
            return true;
        }
    }

    private async Task<TimeSpan> ResolveThrottleWindowAsync(CancellationToken cancellationToken)
    {
        string? raw = await settingProvider
            .GetOrNullAsync(IoTSettingNames.NotificationThrottleMinutes, cancellationToken)
            .ConfigureAwait(false);

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
        {
            minutes = DefaultThrottleMinutes;
        }

        minutes = Math.Clamp(minutes, MinThrottleMinutes, MaxThrottleMinutes);
        return TimeSpan.FromMinutes(minutes);
    }

    [LoggerMessage(EventId = 4301, Level = LogLevel.Warning, Message = "Alert throttle cache unavailable for key '{Key}' (tenant '{TenantId}'). Failing open — notification will be published.")]
    private static partial void LogThrottleCacheUnavailable(ILogger logger, Exception exception, string key, Guid? tenantId);
}
