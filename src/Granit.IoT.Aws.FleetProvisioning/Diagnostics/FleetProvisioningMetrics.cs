using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Granit.IoT.Aws.FleetProvisioning.Diagnostics;

/// <summary>
/// OpenTelemetry counters for the JITP endpoints and the claim-cert
/// rotation check. Tagged with <c>tenant_id</c> (coalesced to
/// <c>"global"</c>) so dashboards can split by tenant.
/// </summary>
public sealed class FleetProvisioningMetrics
{
    public const string MeterName = "Granit.IoT.Aws.FleetProvisioning";

    private readonly Counter<long> _verifyAllowed;
    private readonly Counter<long> _verifyDenied;
    private readonly Counter<long> _registerCompleted;
    private readonly Counter<long> _registerIdempotent;
    private readonly Counter<long> _claimCertExpiring;

    public FleetProvisioningMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        Meter meter = meterFactory.Create(MeterName);

        _verifyAllowed = meter.CreateCounter<long>(
            "granit.iot.aws.jitp.verify_allowed",
            unit: "{verify}",
            description: "Pre-provisioning verifications that allowed the JITP flow to proceed.");
        _verifyDenied = meter.CreateCounter<long>(
            "granit.iot.aws.jitp.verify_denied",
            unit: "{verify}",
            description: "Pre-provisioning verifications that denied the JITP flow (deny-list match).");
        _registerCompleted = meter.CreateCounter<long>(
            "granit.iot.aws.jitp.register_completed",
            unit: "{registration}",
            description: "Post-provisioning registrations that materialised a new Device + AwsThingBinding.");
        _registerIdempotent = meter.CreateCounter<long>(
            "granit.iot.aws.jitp.register_idempotent",
            unit: "{registration}",
            description: "Post-provisioning registrations short-circuited because the Device + binding already existed.");
        _claimCertExpiring = meter.CreateCounter<long>(
            "granit.iot.aws.jitp.claim_certificate_expiring",
            unit: "{certificate}",
            description: "Claim certificates whose expiry is within the configured warning window.");
    }

    public void RecordVerifyAllowed(Guid? tenantId) => _verifyAllowed.Add(1, BuildTags(tenantId));
    public void RecordVerifyDenied(Guid? tenantId) => _verifyDenied.Add(1, BuildTags(tenantId));
    public void RecordRegisterCompleted(Guid? tenantId) => _registerCompleted.Add(1, BuildTags(tenantId));
    public void RecordRegisterIdempotent(Guid? tenantId) => _registerIdempotent.Add(1, BuildTags(tenantId));
    public void RecordClaimCertificateExpiring(Guid? tenantId) => _claimCertExpiring.Add(1, BuildTags(tenantId));

    private static TagList BuildTags(Guid? tenantId) => new()
    {
        { "tenant_id", tenantId?.ToString("N") ?? "global" },
    };
}
