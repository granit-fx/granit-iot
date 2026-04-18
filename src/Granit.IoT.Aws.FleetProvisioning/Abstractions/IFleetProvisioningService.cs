using Granit.IoT.Aws.FleetProvisioning.Contracts;

namespace Granit.IoT.Aws.FleetProvisioning.Abstractions;

/// <summary>
/// Implements the two JITP hook semantics: pre-provisioning verification
/// against the deny-list and post-provisioning materialisation of the
/// <c>Device</c> aggregate plus its <c>AwsThingBinding</c> companion.
/// Both calls are idempotent — a network-retried JITP flow does not create
/// duplicate aggregates or bindings.
/// </summary>
public interface IFleetProvisioningService
{
    /// <summary>
    /// Pre-provisioning hook — evaluates the tenant's serial policy and the
    /// deny-list before AWS materialises the Thing and claim certificate.
    /// </summary>
    Task<FleetProvisioningVerifyResponse> VerifyAsync(
        FleetProvisioningVerifyRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Post-provisioning hook — idempotently materialises the <c>Device</c>
    /// aggregate and its <c>AwsThingBinding</c> companion once AWS has
    /// created the Thing and issued the permanent certificate.
    /// </summary>
    Task<FleetProvisioningRegisterResponse> RegisterAsync(
        FleetProvisioningRegisterRequest request,
        CancellationToken cancellationToken);
}
