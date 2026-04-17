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
    Task<FleetProvisioningVerifyResponse> VerifyAsync(
        FleetProvisioningVerifyRequest request,
        CancellationToken cancellationToken);

    Task<FleetProvisioningRegisterResponse> RegisterAsync(
        FleetProvisioningRegisterRequest request,
        CancellationToken cancellationToken);
}
