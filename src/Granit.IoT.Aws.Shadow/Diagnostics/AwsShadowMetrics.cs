using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Granit.IoT.Aws.Shadow.Diagnostics;

/// <summary>
/// OpenTelemetry counters for the Device Shadow bridge. Tagged with
/// <c>tenant_id</c> (coalesced to <c>"global"</c>) for fleet vs. per-tenant
/// dashboards.
/// </summary>
public sealed class AwsShadowMetrics
{
    public const string MeterName = "Granit.IoT.Aws.Shadow";

    private readonly Counter<long> _reportedPushed;
    private readonly Counter<long> _updateFailed;
    private readonly Counter<long> _deltaDetected;

    public AwsShadowMetrics(IMeterFactory meterFactory)
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

    public void RecordReportedPushed(Guid? tenantId) => _reportedPushed.Add(1, BuildTags(tenantId));

    public void RecordUpdateFailed(Guid? tenantId) => _updateFailed.Add(1, BuildTags(tenantId));

    public void RecordDeltaDetected(Guid? tenantId) => _deltaDetected.Add(1, BuildTags(tenantId));

    private static TagList BuildTags(Guid? tenantId) => new()
    {
        { "tenant_id", tenantId?.ToString("N") ?? "global" },
    };
}
