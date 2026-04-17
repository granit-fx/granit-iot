using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Granit.IoT.Diagnostics;

/// <summary>
/// OpenTelemetry metrics for the IoT module.
/// </summary>
public sealed class IoTMetrics
{
    public const string MeterName = "Granit.IoT";

    private const string TagTenantId = "tenant_id";
    private const string TagSource = "source";
    private const string DefaultTenant = "global";

    private readonly Counter<long> _telemetryIngested;
    private readonly Counter<long> _deviceOfflineDetected;
    private readonly Counter<long> _ingestionSignatureRejected;
    private readonly Counter<long> _ingestionDuplicateSkipped;
    private readonly Counter<long> _ingestionUnknownDevice;
    private readonly Counter<long> _ingestionThresholdExceeded;
    private readonly Counter<long> _alertsThrottled;
    private readonly Counter<long> _telemetryPurged;
    private readonly Counter<long> _partitionCreated;

    public IoTMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);

        _telemetryIngested = meter.CreateCounter<long>(
            "granit.iot.telemetry.ingested",
            description: "Number of telemetry points successfully ingested.");

        _deviceOfflineDetected = meter.CreateCounter<long>(
            "granit.iot.device.offline_detected",
            description: "Number of devices detected as offline by heartbeat timeout.");

        _ingestionSignatureRejected = meter.CreateCounter<long>(
            "granit.iot.ingestion.signature_rejected",
            description: "Number of ingestion requests rejected due to invalid HMAC signature.");

        _ingestionDuplicateSkipped = meter.CreateCounter<long>(
            "granit.iot.ingestion.duplicate_skipped",
            description: "Number of ingestion requests skipped because the transport message id was already seen.");

        _ingestionUnknownDevice = meter.CreateCounter<long>(
            "granit.iot.ingestion.unknown_device",
            description: "Number of telemetry payloads that referenced an unknown device serial number.");

        _ingestionThresholdExceeded = meter.CreateCounter<long>(
            "granit.iot.ingestion.threshold_exceeded",
            description: "Number of telemetry metrics that breached a configured threshold.");

        _alertsThrottled = meter.CreateCounter<long>(
            "granit.iot.alerts.throttled",
            description: "Number of threshold-alert notifications suppressed because an alert for the same (device, metric) was published within the throttle window.");

        _telemetryPurged = meter.CreateCounter<long>(
            "granit.iot.background.telemetry_purged",
            description: "Number of telemetry rows deleted by the stale-telemetry purge job, tagged by tenant.");

        _partitionCreated = meter.CreateCounter<long>(
            "granit.iot.background.partition_created",
            description: "Number of future monthly partitions created by the partition maintenance job.");
    }

    public void RecordTelemetryIngested(string? tenantId, string source) =>
        _telemetryIngested.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { TagSource, source },
        });

    public void RecordDeviceOfflineDetected(string? tenantId) =>
        _deviceOfflineDetected.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
        });

    public void RecordIngestionSignatureRejected(string? tenantId, string source) =>
        _ingestionSignatureRejected.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { TagSource, source },
        });

    public void RecordIngestionDuplicateSkipped(string? tenantId, string source) =>
        _ingestionDuplicateSkipped.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { TagSource, source },
        });

    public void RecordIngestionUnknownDevice(string? tenantId, string source) =>
        _ingestionUnknownDevice.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { TagSource, source },
        });

    public void RecordIngestionThresholdExceeded(string? tenantId, string metricName) =>
        _ingestionThresholdExceeded.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { "metric_name", metricName },
        });

    public void RecordAlertThrottled(string? tenantId, string metricName) =>
        _alertsThrottled.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { "metric_name", metricName },
        });

    public void RecordTelemetryPurged(string? tenantId, long count) =>
        _telemetryPurged.Add(count, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
        });

    public void RecordPartitionCreated(string partitionName) =>
        _partitionCreated.Add(1, new TagList
        {
            { "partition_name", partitionName },
        });
}
