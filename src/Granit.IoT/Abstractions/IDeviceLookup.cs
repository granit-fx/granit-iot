namespace Granit.IoT.Abstractions;

/// <summary>
/// Cross-tenant device lookup used by the ingestion pipeline. Webhook deliveries from
/// IoT hubs are not authenticated against a tenant context (they are signed by a shared
/// secret instead), so the resolver must bypass the multi-tenant filter and locate the
/// device by globally unique serial number.
/// </summary>
public interface IDeviceLookup
{
    /// <summary>
    /// Resolves a device by its serial number across all tenants.
    /// </summary>
    /// <returns>
    /// The matching <see cref="DeviceLookupResult"/>, or <see langword="null"/> if no
    /// device with the given serial number exists.
    /// </returns>
    Task<DeviceLookupResult?> FindBySerialNumberAsync(
        string serialNumber,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal projection used by <see cref="IDeviceLookup"/>. Avoids materializing the
/// whole aggregate when only the identity and tenant are needed.
/// </summary>
public sealed record DeviceLookupResult(Guid DeviceId, Guid? TenantId);
