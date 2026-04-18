using Granit.IoT.Aws.FleetProvisioning.Abstractions;

namespace Granit.IoT.Aws.FleetProvisioning.Internal;

/// <summary>
/// Default <see cref="IFleetProvisioningSerialPolicy"/> — allows every serial.
/// Adopters replace this with their vendor-specific policy (prefix, Luhn,
/// signed bootloader, registry lookup) via
/// <c>services.AddSingleton&lt;IFleetProvisioningSerialPolicy, MyPolicy&gt;()</c>.
/// </summary>
internal sealed class AllowAllSerialPolicy : IFleetProvisioningSerialPolicy
{
    public ValueTask<SerialPolicyDecision> EvaluateAsync(
        string serialNumber,
        Guid? tenantId,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(SerialPolicyDecision.Allow);
}
