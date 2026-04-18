namespace Granit.IoT.Aws.FleetProvisioning.Abstractions;

/// <summary>
/// Optional hook that decides whether a given serial number is eligible for
/// JITP in the current tenant. Adopters plug in their hardware-vendor serial
/// shape (prefix, Luhn, signed bootloader evidence) here; the default
/// implementation allows every non-decommissioned serial so the out-of-the-box
/// behaviour is unchanged.
/// </summary>
/// <remarks>
/// Called <b>before</b> the "already-decommissioned" check in
/// <c>IFleetProvisioningService.VerifyAsync</c>. Rejecting here stops the AWS
/// JITP flow at its pre-hook, so the Thing and claim certificate are never
/// materialised on AWS for an untrusted serial.
/// </remarks>
public interface IFleetProvisioningSerialPolicy
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="serialNumber"/> is eligible for
    /// provisioning in the current tenant. A <c>false</c> result short-circuits
    /// JITP with <c>AllowProvisioning = false</c> and the returned
    /// <see cref="SerialPolicyDecision.DenyReason"/> is surfaced to the caller.
    /// </summary>
    ValueTask<SerialPolicyDecision> EvaluateAsync(
        string serialNumber,
        Guid? tenantId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Decision returned by <see cref="IFleetProvisioningSerialPolicy"/>.
/// <see cref="Allow"/> is the accepting verdict; <see cref="Deny(string)"/>
/// short-circuits the flow with the supplied reason.
/// </summary>
public readonly record struct SerialPolicyDecision(bool Allowed, string? DenyReason)
{
    /// <summary>Allow verdict — the serial is eligible for provisioning.</summary>
    public static SerialPolicyDecision Allow { get; } = new(true, null);

    /// <summary>Deny verdict — the JITP flow is short-circuited and <paramref name="reason"/> is surfaced to the caller.</summary>
    public static SerialPolicyDecision Deny(string reason) => new(false, reason);
}
