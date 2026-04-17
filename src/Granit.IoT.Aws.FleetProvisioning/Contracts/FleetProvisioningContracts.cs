using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Aws.FleetProvisioning.Contracts;

/// <summary>
/// Pre-provisioning hook payload sent by the customer's AWS Lambda when a
/// device claim-cert authentication reaches AWS IoT. The serial number is
/// the only required field — everything else is optional metadata that
/// helps the deny-list decision.
/// </summary>
public sealed record FleetProvisioningVerifyRequest(
    [property: Required(AllowEmptyStrings = false)]
    string SerialNumber,
    Guid? TenantId);

/// <summary>
/// Verdict returned to the AWS pre-provisioning hook. AWS aborts the JITP
/// flow when <see cref="AllowProvisioning"/> is <c>false</c>.
/// </summary>
public sealed record FleetProvisioningVerifyResponse(
    bool AllowProvisioning,
    string? Reason);

/// <summary>
/// Post-provisioning hook payload sent by the customer's AWS Lambda once
/// AWS IoT has created the Thing and the operational certificate. The
/// caller is responsible for whatever Secrets Manager strategy applies —
/// <see cref="CertificateSecretArn"/> can point at a real secret or at a
/// placeholder ARN when the device is the sole holder of the private key.
/// </summary>
public sealed record FleetProvisioningRegisterRequest(
    [property: Required(AllowEmptyStrings = false)]
    string SerialNumber,
    Guid? TenantId,
    [property: Required(AllowEmptyStrings = false)]
    string ThingName,
    [property: Required(AllowEmptyStrings = false)]
    string ThingArn,
    [property: Required(AllowEmptyStrings = false)]
    string CertificateArn,
    [property: Required(AllowEmptyStrings = false)]
    string CertificateSecretArn,
    [property: Required(AllowEmptyStrings = false)]
    string Model,
    [property: Required(AllowEmptyStrings = false)]
    string FirmwareVersion,
    string? Label,
    DateTimeOffset? ClaimCertificateExpiresAt);

/// <summary>
/// Acknowledgment returned to the AWS post-provisioning hook with the id
/// of the freshly created (or already existing) <c>Device</c> aggregate.
/// </summary>
public sealed record FleetProvisioningRegisterResponse(
    Guid DeviceId,
    bool AlreadyProvisioned);
