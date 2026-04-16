using System.Globalization;
using Granit.IoT.Events;
using Granit.IoT.Wolverine.Abstractions;
using Granit.Settings.Services;

namespace Granit.IoT.Wolverine.Internal;

/// <summary>
/// Default <see cref="IDeviceThresholdEvaluator"/> backed by <c>Granit.Settings</c>.
/// Looks up <c>IoT:Threshold:{metricName}</c> for each metric carried by the ingested
/// payload. Settings cascade automatically (User → Tenant → Global → Configuration → Default).
/// </summary>
internal sealed class SettingsDeviceThresholdEvaluator(ISettingProvider settingProvider) : IDeviceThresholdEvaluator
{
    private const string SettingKeyPrefix = "IoT:Threshold:";

    public async Task<IReadOnlyList<TelemetryThresholdExceededEto>> EvaluateAsync(
        Guid deviceId,
        Guid? tenantId,
        IReadOnlyDictionary<string, double> metrics,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        if (metrics.Count == 0)
        {
            return [];
        }

        List<TelemetryThresholdExceededEto>? breaches = null;

        foreach (KeyValuePair<string, double> metric in metrics)
        {
            string? raw = await settingProvider
                .GetOrNullAsync(string.Concat(SettingKeyPrefix, metric.Key), cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double threshold))
            {
                continue;
            }

            if (metric.Value > threshold)
            {
                breaches ??= [];
                breaches.Add(new TelemetryThresholdExceededEto(
                    deviceId,
                    tenantId,
                    metric.Key,
                    metric.Value,
                    threshold,
                    recordedAt));
            }
        }

        return breaches is null ? [] : breaches;
    }
}
