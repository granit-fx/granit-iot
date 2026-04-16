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
}
