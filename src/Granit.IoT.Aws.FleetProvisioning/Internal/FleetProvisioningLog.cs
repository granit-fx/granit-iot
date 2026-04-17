using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.FleetProvisioning.Internal;

internal static partial class FleetProvisioningLog
{
    [LoggerMessage(EventId = 5201, Level = LogLevel.Information,
        Message = "Fleet provisioning verify denied for serial '{serialNumber}': device is decommissioned.")]
    public static partial void VerifyDeniedDecommissioned(ILogger logger, string serialNumber);

    [LoggerMessage(EventId = 5202, Level = LogLevel.Information,
        Message = "Fleet provisioning register completed for serial '{serialNumber}' (deviceId {deviceId}).")]
    public static partial void RegisterCompleted(ILogger logger, string serialNumber, Guid deviceId);

    [LoggerMessage(EventId = 5203, Level = LogLevel.Information,
        Message = "Fleet provisioning register reused existing device for serial '{serialNumber}' (deviceId {deviceId}).")]
    public static partial void RegisterIdempotent(ILogger logger, string serialNumber, Guid deviceId);

    [LoggerMessage(EventId = 5204, Level = LogLevel.Warning,
        Message = "Claim certificate for Thing '{thingName}' expires in {daysUntilExpiry} day(s) (at {expiresAt:O}).")]
    public static partial void ClaimCertificateExpiring(ILogger logger, string thingName, int daysUntilExpiry, DateTimeOffset expiresAt);

    [LoggerMessage(EventId = 5205, Level = LogLevel.Warning,
        Message = "Claim certificate rotation tick failed; will retry on next interval.")]
    public static partial void RotationTickFailed(ILogger logger, Exception exception);
}
