namespace Granit.IoT.Abstractions;

/// <summary>
/// Bulk-deletes telemetry rows for retention enforcement (GDPR right to erasure).
/// Implemented in <c>Granit.IoT.EntityFrameworkCore</c> and consumed by
/// <c>StaleTelemetryPurgeJob</c>. Bypasses the multi-tenancy query filter so a
/// single call can purge across multiple tenants in one SQL statement.
/// </summary>
public interface ITelemetryPurger
{
    /// <summary>
    /// Deletes telemetry rows where <c>TenantId</c> is in <paramref name="tenantIds"/>
    /// and <c>RecordedAt</c> is strictly less than <paramref name="cutoff"/>. Returns
    /// the number of rows removed.
    /// </summary>
    Task<long> PurgeOlderThanAsync(
        IReadOnlyCollection<Guid?> tenantIds,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default);
}
