using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Granit.IoT.Aws.Shadow.Diagnostics;

/// <summary>
/// OpenTelemetry counters for the Device Shadow bridge. Tagged with
/// <c>tenant_id</c> (coalesced to <c>"global"</c>) for fleet vs. per-tenant
/// dashboards.
/// </summary>
public sealed class IoTAwsShadowMetrics
{
    /// <summary>Meter name used by OpenTelemetry exporters.</summary>
    public const string MeterName = "Granit.IoT.Aws.Shadow";

    private readonly Counter<long> _reportedPushed;
    private readonly Counter<long> _updateFailed;
    private readonly Counter<long> _deltaDetected;

    /// <summary>Creates the metrics instance and registers every counter against the shared meter.</summary>
    /// <param name="meterFactory">Factory used to create the <see cref="Meter"/> instance.</param>
    public IoTAwsShadowMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        Meter meter = meterFactory.Create(MeterName);

        _reportedPushed = meter.CreateCounter<long>(
            "granit.iot.aws.shadow.reported_pushed",
            unit: "{push}",
            description: "Reported-state pushes sent to AWS IoT Device Shadow.");
        _updateFailed = meter.CreateCounter<long>(
            "granit.iot.aws.shadow.update_failures",
            unit: "{push}",
            description: "Failures of UpdateThingShadow (network, throttling, IoT Data plane unavailable).");
        _deltaDetected = meter.CreateCounter<long>(
            "granit.iot.aws.shadow.delta_detected",
            unit: "{delta}",
            description: "Non-empty desired/reported deltas observed by the polling service.");
    }

    /// <summary>Records a reported-state push sent to AWS IoT Device Shadow.</summary>
    public void RecordReportedPushed(Guid? tenantId) => _reportedPushed.Add(1, BuildTags(tenantId));

    /// <summary>Records a failure of <c>UpdateThingShadow</c> (network, throttling, IoT Data plane unavailable).</summary>
    public void RecordUpdateFailed(Guid? tenantId) => _updateFailed.Add(1, BuildTags(tenantId));

    /// <summary>Records a non-empty desired/reported delta observed by the polling service.</summary>
    public void RecordDeltaDetected(Guid? tenantId) => _deltaDetected.Add(1, BuildTags(tenantId));

    private static TagList BuildTags(Guid? tenantId) => new()
    {
        { "tenant_id", tenantId?.ToString("N") ?? "global" },
    };
}
