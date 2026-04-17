using Granit.IoT.Aws.Domain;

namespace Granit.IoT.Aws.Provisioning.Abstractions;

/// <summary>
/// Idempotent operations that walk an <see cref="AwsThingBinding"/> through
/// the AWS-side provisioning saga. Each method is safe to call when the
/// binding has already passed the matching checkpoint — it short-circuits
/// without re-issuing the corresponding AWS call.
/// </summary>
/// <remarks>
/// Implementations defensively call <c>Describe*</c> APIs before attempting
/// a create, which means that a crash between an AWS-side success and the
/// matching DB commit recovers cleanly on replay.
/// </remarks>
public interface IThingProvisioningService
{
    /// <summary>
    /// Step 1: ensures the AWS IoT Thing exists. Mutates
    /// <c>binding.ProvisioningStatus</c> to <c>ThingCreated</c> with the
    /// AWS ARN. Caller must persist the binding after the call returns.
    /// </summary>
    Task EnsureThingAsync(AwsThingBinding binding, CancellationToken cancellationToken);

    /// <summary>
    /// Steps 2 + 3 fused: issues the X.509 certificate AND persists its
    /// private key to Secrets Manager in the same logical operation. Splitting
    /// these into two transactions would risk losing the (un-recoverable)
    /// private key if a crash happened in between. On success the binding
    /// state advances to <c>SecretStored</c>.
    /// </summary>
    Task EnsureCertificateAndSecretAsync(AwsThingBinding binding, CancellationToken cancellationToken);

    /// <summary>
    /// Step 4: attaches the configured device policy to the certificate and
    /// binds the certificate to the Thing principal. Idempotent: AWS rejects
    /// nothing if both attachments already exist.
    /// </summary>
    Task EnsureActivationAsync(AwsThingBinding binding, CancellationToken cancellationToken);

    /// <summary>
    /// Decommissioning path: detach principal/policy, deactivate certificate,
    /// delete Thing + certificate + secret. Idempotent — missing AWS
    /// resources are treated as already-deleted.
    /// </summary>
    Task DecommissionAsync(AwsThingBinding binding, CancellationToken cancellationToken);
}
