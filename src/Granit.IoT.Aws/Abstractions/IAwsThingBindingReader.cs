using Granit.IoT.Aws.Domain;

namespace Granit.IoT.Aws.Abstractions;

/// <summary>Reads <see cref="AwsThingBinding"/> rows (query side of CQRS).</summary>
public interface IAwsThingBindingReader
{
    /// <summary>Returns the binding for a given device, or <c>null</c> if absent (tenant-scoped).</summary>
    Task<AwsThingBinding?> FindByDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the binding matching a given <see cref="ThingName"/>, or <c>null</c>.</summary>
    Task<AwsThingBinding?> FindByThingNameAsync(ThingName thingName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns bindings whose status is one of <paramref name="statuses"/>. Used by
    /// the reconciliation tooling to surface stuck (<c>Pending</c>, <c>Failed</c>)
    /// or expiring (<c>ClaimCertificateExpiresAt</c>) entries.
    /// </summary>
    Task<IReadOnlyList<AwsThingBinding>> ListByStatusAsync(
        IReadOnlyCollection<AwsThingProvisioningStatus> statuses,
        int batchSize = 100,
        CancellationToken cancellationToken = default);
}
