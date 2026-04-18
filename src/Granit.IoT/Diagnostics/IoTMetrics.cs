using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Granit.IoT.Diagnostics;

/// <summary>
/// OpenTelemetry metrics for the IoT module.
/// </summary>
public sealed class IoTMetrics
{
    /// <summary>Meter name used by OpenTelemetry exporters.</summary>
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
    private readonly Counter<long> _dedupFailOpen;

    /// <summary>Creates the metrics instance and registers every counter against the shared meter.</summary>
    /// <param name="meterFactory">Factory used to create the <see cref="Meter"/> instance (required for proper OpenTelemetry wiring).</param>
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

        _dedupFailOpen = meter.CreateCounter<long>(
            "granit.iot.ingestion.dedup_fail_open",
            description: "Number of ingestion requests processed without deduplication because the idempotency store was unavailable. A sustained non-zero rate indicates a replay-window integrity gap — page on-call.");
    }

    /// <summary>Records a telemetry point that was successfully ingested, tagged with tenant and source.</summary>
    public void RecordTelemetryIngested(string? tenantId, string source) =>
        _telemetryIngested.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { TagSource, source },
        });

    /// <summary>Records a device detected as offline by the heartbeat-timeout job.</summary>
    public void RecordDeviceOfflineDetected(string? tenantId) =>
        _deviceOfflineDetected.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
        });

    /// <summary>Records an inbound ingestion request rejected because the provider signature did not verify.</summary>
    public void RecordIngestionSignatureRejected(string? tenantId, string source) =>
        _ingestionSignatureRejected.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { TagSource, source },
        });

    /// <summary>Records an ingestion request short-circuited by transport-level deduplication (already-seen message id).</summary>
    public void RecordIngestionDuplicateSkipped(string? tenantId, string source) =>
        _ingestionDuplicateSkipped.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { TagSource, source },
        });

    /// <summary>Records a telemetry payload whose device serial number is not registered in the current tenant.</summary>
    public void RecordIngestionUnknownDevice(string? tenantId, string source) =>
        _ingestionUnknownDevice.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
            { TagSource, source },
        });

    /// <summary>
    /// Records a telemetry metric that breached a configured threshold. The metric name is
    /// <em>not</em> emitted as a tag — device-controlled metric keys would blow Prometheus
    /// cardinality. Use structured logs for per-metric breakdowns.
    /// </summary>
    public void RecordIngestionThresholdExceeded(string? tenantId, string metricName) =>
        _ingestionThresholdExceeded.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
        });

    /// <summary>Records a threshold alert suppressed by the throttle window (same device + metric fired recently).</summary>
    public void RecordAlertThrottled(string? tenantId, string metricName) =>
        _alertsThrottled.Add(1, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
        });

    /// <summary>Records the number of rows deleted by the stale-telemetry purge job.</summary>
    /// <param name="tenantId">Tenant whose telemetry was purged.</param>
    /// <param name="count">Number of rows deleted in this run.</param>
    public void RecordTelemetryPurged(string? tenantId, long count) =>
        _telemetryPurged.Add(count, new TagList
        {
            { TagTenantId, tenantId ?? DefaultTenant },
        });

    /// <summary>Records the creation of a future monthly partition by the partition-maintenance job.</summary>
    /// <param name="partitionName">Name of the created partition (e.g. <c>iot_telemetry_points_2026_05</c>).</param>
    public void RecordPartitionCreated(string partitionName) =>
        _partitionCreated.Add(1, new TagList
        {
            { "partition_name", partitionName },
        });

    /// <summary>
    /// Records an ingestion request that bypassed deduplication because the idempotency
    /// store was unavailable (fail-open). Sustained non-zero rate = replay-window gap.
    /// </summary>
    public void RecordDedupFailOpen() => _dedupFailOpen.Add(1);
}
