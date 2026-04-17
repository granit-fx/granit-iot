using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Granit.IoT.Aws.Provisioning.Diagnostics;

/// <summary>
/// OpenTelemetry counters that track the saga's progress. Tagged with
/// <c>tenant_id</c> (coalesced to <c>"global"</c> when null) so dashboards
/// can split by tenant or aggregate across the fleet.
/// </summary>
public sealed class AwsProvisioningMetrics
{
    public const string MeterName = "Granit.IoT.Aws.Provisioning";

    private readonly Counter<long> _thingCreated;
    private readonly Counter<long> _certIssued;
    private readonly Counter<long> _activated;
    private readonly Counter<long> _decommissioned;
    private readonly Counter<long> _failed;

    public AwsProvisioningMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        Meter meter = meterFactory.Create(MeterName);

        _thingCreated = meter.CreateCounter<long>(
            "granit.iot.aws.thing.created",
            unit: "{thing}",
            description: "AWS IoT Things created by the provisioning saga.");
        _certIssued = meter.CreateCounter<long>(
            "granit.iot.aws.certificate.issued",
            unit: "{certificate}",
            description: "X.509 device certificates issued by the provisioning saga.");
        _activated = meter.CreateCounter<long>(
            "granit.iot.aws.binding.activated",
            unit: "{binding}",
            description: "AwsThingBindings that reached the Active status.");
        _decommissioned = meter.CreateCounter<long>(
            "granit.iot.aws.binding.decommissioned",
            unit: "{binding}",
            description: "AwsThingBindings decommissioned (Thing/cert/secret removed from AWS).");
        _failed = meter.CreateCounter<long>(
            "granit.iot.aws.provisioning.failed",
            unit: "{binding}",
            description: "Saga failures requiring operator reconciliation.");
    }

    public void RecordThingCreated(Guid? tenantId) => _thingCreated.Add(1, BuildTags(tenantId));

    public void RecordCertificateIssued(Guid? tenantId) => _certIssued.Add(1, BuildTags(tenantId));

    public void RecordActivated(Guid? tenantId) => _activated.Add(1, BuildTags(tenantId));

    public void RecordDecommissioned(Guid? tenantId) => _decommissioned.Add(1, BuildTags(tenantId));

    public void RecordFailed(Guid? tenantId) => _failed.Add(1, BuildTags(tenantId));

    private static TagList BuildTags(Guid? tenantId) => new()
    {
        { "tenant_id", tenantId?.ToString("N") ?? "global" },
    };
}
