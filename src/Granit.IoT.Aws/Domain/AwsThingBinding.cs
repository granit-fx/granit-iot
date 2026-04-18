using Granit.Domain;
using Granit.IoT.Aws.Events;

namespace Granit.IoT.Aws.Domain;

/// <summary>
/// 1:1 companion of the cloud-agnostic <c>Device</c> aggregate carrying the
/// AWS IoT projection: Thing/Certificate ARNs, Secrets Manager ARN of the
/// private key, and the saga checkpoint of the provisioning workflow.
/// Lifecycle is driven by the bridge handlers reacting to <c>Device</c>
/// domain events — the core <c>Device</c> never references this type.
/// </summary>
public sealed class AwsThingBinding : FullAuditedAggregateRoot, IMultiTenant
{
    private AwsThingBinding() { }

    /// <summary>Foreign key to <c>Device.Id</c> (1:1 unique index in the database).</summary>
    public Guid DeviceId { get; private set; }

    /// <summary>AWS IoT Thing name derived from <c>t{tenantId:N}-{serialNumber}</c>.</summary>
    public ThingName ThingName { get; private set; } = null!;

    /// <summary>AWS ARN of the IoT Thing. Null until <see cref="RecordThingCreated"/>.</summary>
    public string? ThingArn { get; private set; }

    /// <summary>AWS ARN of the X.509 device certificate. Null until <see cref="RecordCertificateIssued"/>.</summary>
    public string? CertificateArn { get; private set; }

    /// <summary>AWS Secrets Manager ARN holding the private key. Null until <see cref="RecordSecretStored"/>.</summary>
    public string? CertificateSecretArn { get; private set; }

    /// <summary>Current saga checkpoint (Pending → ThingCreated → CertIssued → SecretStored → Active, with terminal Decommissioned / Failed).</summary>
    public AwsThingProvisioningStatus ProvisioningStatus { get; private set; }

    /// <summary>Last time the device pushed its <c>reported</c> shadow state.</summary>
    public DateTimeOffset? LastShadowReportedAt { get; private set; }

    /// <summary>
    /// Expiry of the AWS IoT claim certificate used by Fleet Provisioning (JITP).
    /// Tracked by <c>ClaimCertificateRotationCheckJob</c> (story #50) so an alert
    /// fires before the JITP path silently breaks.
    /// </summary>
    public DateTimeOffset? ClaimCertificateExpiresAt { get; private set; }

    /// <summary>True when this binding was created through Fleet Provisioning (JITP).</summary>
    public bool ProvisionedViaJitp { get; private set; }

    /// <summary>Non-null when <see cref="ProvisioningStatus"/> is <see cref="AwsThingProvisioningStatus.Failed"/>; carries the reason recorded by <see cref="MarkAsFailed"/>.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Tenant that owns the binding. Stamped at construction from the server-side
    /// current-tenant context — never from client-supplied JITP body fields.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Explicit <see cref="IMultiTenant.TenantId"/> implementation so only the
    /// Granit audit interceptor can write this field during persistence.
    /// </summary>
    Guid? IMultiTenant.TenantId
    {
        get => TenantId;
        set => TenantId = value;
    }

    /// <summary>
    /// Reserves a binding row before any AWS API call. Status is
    /// <see cref="AwsThingProvisioningStatus.Pending"/>; the bridge handler
    /// then progresses through the saga via the <c>Record*</c> methods.
    /// </summary>
    public static AwsThingBinding Create(Guid deviceId, Guid? tenantId, ThingName thingName)
    {
        ArgumentNullException.ThrowIfNull(thingName);
        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException("Device id must not be empty.", nameof(deviceId));
        }

        return new AwsThingBinding
        {
            DeviceId = deviceId,
            TenantId = tenantId,
            ThingName = thingName,
            ProvisioningStatus = AwsThingProvisioningStatus.Pending,
        };
    }

    /// <summary>
    /// Creates a binding already in <see cref="AwsThingProvisioningStatus.Active"/>
    /// for the Fleet Provisioning (JITP) flow, where AWS has already created
    /// the Thing and certificate before calling Granit. Bypasses the standard
    /// bridge handler entirely; raises <see cref="AwsThingProvisionedEvent"/>.
    /// </summary>
    public static AwsThingBinding CreateForJitp(
        Guid deviceId,
        Guid? tenantId,
        ThingName thingName,
        string thingArn,
        string certificateArn,
        string certificateSecretArn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingArn);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateArn);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateSecretArn);

        AwsThingBinding binding = Create(deviceId, tenantId, thingName);
        binding.ThingArn = thingArn;
        binding.CertificateArn = certificateArn;
        binding.CertificateSecretArn = certificateSecretArn;
        binding.ProvisioningStatus = AwsThingProvisioningStatus.Active;
        binding.ProvisionedViaJitp = true;
        binding.AddDomainEvent(new AwsThingProvisionedEvent(deviceId, thingName, thingArn, tenantId));
        return binding;
    }

    /// <summary>Saga step 1: <c>CreateThing</c> succeeded. Idempotent.</summary>
    public void RecordThingCreated(string thingArn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingArn);
        EnsureNotTerminal(nameof(RecordThingCreated));
        if (IsAtLeast(AwsThingProvisioningStatus.ThingCreated))
        {
            return;
        }

        ThingArn = thingArn;
        ProvisioningStatus = AwsThingProvisioningStatus.ThingCreated;
    }

    /// <summary>Saga step 2: <c>CreateKeysAndCertificate</c> succeeded. Idempotent.</summary>
    public void RecordCertificateIssued(string certificateArn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateArn);
        EnsureNotTerminal(nameof(RecordCertificateIssued));
        if (IsAtLeast(AwsThingProvisioningStatus.CertIssued))
        {
            return;
        }

        EnsureStatus(AwsThingProvisioningStatus.ThingCreated, nameof(RecordCertificateIssued));
        CertificateArn = certificateArn;
        ProvisioningStatus = AwsThingProvisioningStatus.CertIssued;
    }

    /// <summary>Saga step 3: private key persisted in Secrets Manager. Idempotent.</summary>
    public void RecordSecretStored(string secretArn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretArn);
        EnsureNotTerminal(nameof(RecordSecretStored));
        if (IsAtLeast(AwsThingProvisioningStatus.SecretStored))
        {
            return;
        }

        EnsureStatus(AwsThingProvisioningStatus.CertIssued, nameof(RecordSecretStored));
        CertificateSecretArn = secretArn;
        ProvisioningStatus = AwsThingProvisioningStatus.SecretStored;
    }

    /// <summary>
    /// Saga step 4: IoT policy attached and certificate bound to the Thing —
    /// the device is ready to connect. Raises <see cref="AwsThingProvisionedEvent"/>.
    /// </summary>
    public void MarkAsActive()
    {
        EnsureNotTerminal(nameof(MarkAsActive));
        if (IsAtLeast(AwsThingProvisioningStatus.Active))
        {
            return;
        }

        EnsureStatus(AwsThingProvisioningStatus.SecretStored, nameof(MarkAsActive));
        ProvisioningStatus = AwsThingProvisioningStatus.Active;
        AddDomainEvent(new AwsThingProvisionedEvent(DeviceId, ThingName, ThingArn!, TenantId));
    }

    /// <summary>
    /// True when the binding has reached or passed <paramref name="checkpoint"/>
    /// in the forward saga. Decommissioned/Failed are terminal states tested
    /// separately via <see cref="EnsureNotTerminal"/>; do not include them in
    /// the forward ordering.
    /// </summary>
    private bool IsAtLeast(AwsThingProvisioningStatus checkpoint) =>
        ProvisioningStatus >= checkpoint
        && ProvisioningStatus <= AwsThingProvisioningStatus.Active;

    /// <summary>
    /// Marks the binding as decommissioned after the matching AWS resources
    /// have been deleted. Raises <see cref="AwsThingDecommissionedEvent"/>.
    /// </summary>
    public void MarkAsDecommissioned()
    {
        if (ProvisioningStatus is AwsThingProvisioningStatus.Decommissioned)
        {
            return;
        }

        ProvisioningStatus = AwsThingProvisioningStatus.Decommissioned;
        AddDomainEvent(new AwsThingDecommissionedEvent(DeviceId, ThingName, TenantId));
    }

    /// <summary>
    /// Marks the binding as non-recoverable. Used when a saga step encounters an
    /// error that idempotent retries cannot resolve (e.g. <c>ThingAlreadyExists</c>
    /// with a different ARN). Operator intervention required.
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ProvisioningStatus = AwsThingProvisioningStatus.Failed;
        FailureReason = reason;
    }

    /// <summary>Records the timestamp of the last <c>reported</c>-state push from the device shadow.</summary>
    public void RecordShadowReportedAt(DateTimeOffset at) => LastShadowReportedAt = at;

    /// <summary>Records the expiry of the JITP claim certificate so the rotation job can raise an alert before it elapses.</summary>
    public void RecordClaimCertificateExpiry(DateTimeOffset expiresAt) => ClaimCertificateExpiresAt = expiresAt;

    private void EnsureStatus(AwsThingProvisioningStatus expected, string operation)
    {
        if (ProvisioningStatus != expected)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} when binding is in '{ProvisioningStatus}' status. Expected '{expected}'.");
        }
    }

    private void EnsureNotTerminal(string operation)
    {
        if (ProvisioningStatus is AwsThingProvisioningStatus.Decommissioned or AwsThingProvisioningStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} when binding is in terminal status '{ProvisioningStatus}'.");
        }
    }
}
