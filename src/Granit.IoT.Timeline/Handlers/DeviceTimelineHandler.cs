using System.Globalization;
using Granit.IoT.Events;
using Granit.Timeline.Abstractions;
using Granit.Timeline.Domain;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Timeline.Handlers;

/// <summary>
/// Wolverine handlers that write a <see cref="TimelineEntryType.SystemLog"/>
/// entry per device-lifecycle domain event. Entry type is "Device" with the
/// device id as entity id, so the existing
/// <c>GET /api/granit/timeline/Device/{id}</c> endpoint surfaces the full
/// chronological audit trail.
/// </summary>
public static partial class DeviceTimelineHandler
{
    private const string EntityType = "Device";

    /// <summary>Handles <see cref="DeviceProvisionedEvent"/> — writes a "provisioned" timeline entry for the device.</summary>
    public static Task HandleAsync(
        DeviceProvisionedEvent e,
        ITimelineWriter writer,
        ILogger<DeviceTimelineHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(e);
        LogEntry(logger, "provisioned", e.DeviceId);
        return writer.PostEntryAsync(
            EntityType,
            FormatId(e.DeviceId),
            TimelineEntryType.SystemLog,
            $"Device provisioned: {e.SerialNumber} (status = Provisioning).",
            cancellationToken: cancellationToken);
    }

    /// <summary>Handles <see cref="DeviceActivatedEvent"/> — writes an "activated" timeline entry for the device.</summary>
    public static Task HandleAsync(
        DeviceActivatedEvent e,
        ITimelineWriter writer,
        ILogger<DeviceTimelineHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(e);
        LogEntry(logger, "activated", e.DeviceId);
        return writer.PostEntryAsync(
            EntityType,
            FormatId(e.DeviceId),
            TimelineEntryType.SystemLog,
            $"Device activated: {e.SerialNumber} (Provisioning -> Active).",
            cancellationToken: cancellationToken);
    }

    /// <summary>Handles <see cref="DeviceSuspendedEvent"/> — writes a "suspended" timeline entry with the supplied reason.</summary>
    public static Task HandleAsync(
        DeviceSuspendedEvent e,
        ITimelineWriter writer,
        ILogger<DeviceTimelineHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(e);
        LogEntry(logger, "suspended", e.DeviceId);
        return writer.PostEntryAsync(
            EntityType,
            FormatId(e.DeviceId),
            TimelineEntryType.SystemLog,
            $"Device suspended (Active -> Suspended): reason = '{e.Reason}'.",
            cancellationToken: cancellationToken);
    }

    /// <summary>Handles <see cref="DeviceReactivatedEvent"/> — writes a "reactivated" timeline entry for the device.</summary>
    public static Task HandleAsync(
        DeviceReactivatedEvent e,
        ITimelineWriter writer,
        ILogger<DeviceTimelineHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(e);
        LogEntry(logger, "reactivated", e.DeviceId);
        return writer.PostEntryAsync(
            EntityType,
            FormatId(e.DeviceId),
            TimelineEntryType.SystemLog,
            $"Device reactivated: {e.SerialNumber} (Suspended -> Active).",
            cancellationToken: cancellationToken);
    }

    /// <summary>Handles <see cref="DeviceDecommissionedEvent"/> — writes a "decommissioned" timeline entry for the device.</summary>
    public static Task HandleAsync(
        DeviceDecommissionedEvent e,
        ITimelineWriter writer,
        ILogger<DeviceTimelineHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(e);
        LogEntry(logger, "decommissioned", e.DeviceId);
        return writer.PostEntryAsync(
            EntityType,
            FormatId(e.DeviceId),
            TimelineEntryType.SystemLog,
            "Device decommissioned (status = Decommissioned).",
            cancellationToken: cancellationToken);
    }

    private static string FormatId(Guid deviceId) => deviceId.ToString("N", CultureInfo.InvariantCulture);

    [LoggerMessage(EventId = 4400, Level = LogLevel.Debug,
        Message = "Timeline entry written for device {DeviceId} ({Lifecycle}).")]
    private static partial void LogEntry(ILogger logger, string lifecycle, Guid deviceId);
}

/// <summary>Marker type for the static handler's logger category.</summary>
public sealed class DeviceTimelineHandlerCategory;
