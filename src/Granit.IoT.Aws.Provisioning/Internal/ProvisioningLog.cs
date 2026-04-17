using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.Provisioning.Internal;

internal static partial class ProvisioningLog
{
    [LoggerMessage(EventId = 4801, Level = LogLevel.Information,
        Message = "AWS IoT Thing '{thingName}' created.")]
    public static partial void ThingCreated(ILogger logger, string thingName);

    [LoggerMessage(EventId = 4802, Level = LogLevel.Information,
        Message = "AWS IoT Thing '{thingName}' already exists; reusing.")]
    public static partial void ThingAlreadyExists(ILogger logger, string thingName);

    [LoggerMessage(EventId = 4803, Level = LogLevel.Information,
        Message = "AWS IoT certificate '{certificateId}' issued for Thing '{thingName}'.")]
    public static partial void CertificateIssued(ILogger logger, string thingName, string certificateId);

    [LoggerMessage(EventId = 4804, Level = LogLevel.Information,
        Message = "AWS IoT binding for Thing '{thingName}' marked Active.")]
    public static partial void BindingActivated(ILogger logger, string thingName);

    [LoggerMessage(EventId = 4805, Level = LogLevel.Information,
        Message = "AWS IoT binding for Thing '{thingName}' decommissioned.")]
    public static partial void BindingDecommissioned(ILogger logger, string thingName);

    [LoggerMessage(EventId = 4806, Level = LogLevel.Warning,
        Message = "AWS IoT bridge handler skipped device {deviceId}: no AwsThingBinding found and reservation failed.")]
    public static partial void ReservationFailed(ILogger logger, Guid deviceId);

    [LoggerMessage(EventId = 4807, Level = LogLevel.Error,
        Message = "AWS IoT provisioning failed for Thing '{thingName}'; binding marked Failed for manual reconciliation.")]
    public static partial void ProvisioningFailed(ILogger logger, string thingName, Exception exception);
}
