using Granit.IoT.Domain;

namespace Granit.IoT.Abstractions;

/// <summary>
/// Host-supplied bridge between a GDPR data subject (a natural person) and
/// the IoT devices that carry their personal data. The <c>Device</c> domain
/// model binds devices to tenants, not to users — so the actual user↔device
/// mapping is host-specific (HR system, wearable registry, asset catalog).
/// Wire an implementation when the host joins <c>Granit.Privacy</c>'s
/// export/deletion sagas so the IoT module can contribute to the subject's
/// package and erasure.
/// </summary>
/// <remarks>
/// Default binding is <see cref="Internal.NullIoTDataSubjectLocator"/>, which
/// returns an empty set — the IoT module contributes zero bytes to GDPR
/// requests until the host wires up a real locator. This keeps the module
/// compliant-by-default (no personal data is claimed) and surfaces an
/// explicit integration decision for tenants that need it.
/// </remarks>
public interface IIoTDataSubjectLocator
{
    /// <summary>
    /// Returns the serial numbers of devices owned by <paramref name="userId"/>.
    /// The IoT module joins these with its <c>Device</c> / <c>TelemetryPoint</c>
    /// stores to produce the export fragment or execute the deletion.
    /// </summary>
    ValueTask<IReadOnlyCollection<DeviceSerialNumber>> LocateDevicesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken);
}
