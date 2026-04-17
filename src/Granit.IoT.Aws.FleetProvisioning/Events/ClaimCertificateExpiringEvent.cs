using Granit.Events;

namespace Granit.IoT.Aws.FleetProvisioning.Events;

/// <summary>
/// Raised by the daily rotation check when a binding's recorded
/// <c>ClaimCertificateExpiresAt</c> falls inside the
/// <c>ExpiryWarningWindowDays</c>. Operators must rotate the JITP claim
/// certificate before this date or new fleet enrolments break.
/// </summary>
public sealed record ClaimCertificateExpiringEvent(
    Guid DeviceId,
    string ThingName,
    DateTimeOffset ExpiresAt,
    int DaysUntilExpiry,
    Guid? TenantId) : IDomainEvent;
