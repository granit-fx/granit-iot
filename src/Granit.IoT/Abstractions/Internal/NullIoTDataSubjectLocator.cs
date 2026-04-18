using Granit.IoT.Domain;

namespace Granit.IoT.Abstractions.Internal;

/// <summary>
/// Default <see cref="IIoTDataSubjectLocator"/> — returns an empty collection
/// so hosts that have not wired a real locator contribute nothing to GDPR
/// export/erasure flows (and cannot silently miss data by mis-registration).
/// </summary>
internal sealed class NullIoTDataSubjectLocator : IIoTDataSubjectLocator
{
    public ValueTask<IReadOnlyCollection<DeviceSerialNumber>> LocateDevicesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyCollection<DeviceSerialNumber>>(Array.Empty<DeviceSerialNumber>());
}
