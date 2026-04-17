using System.Globalization;
using Granit.IoT.Events;
using Granit.IoT.Timeline.Handlers;
using Granit.Timeline.Abstractions;
using Granit.Timeline.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Granit.IoT.Timeline.Tests;

public sealed class DeviceTimelineHandlerTests
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static string DeviceIdN => DeviceId.ToString("N", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Provisioned_WritesSystemLogEntryWithSerialAndStatus()
    {
        ITimelineWriter writer = Substitute.For<ITimelineWriter>();
        var evt = new DeviceProvisionedEvent(DeviceId, "SN-001", TenantId: null);

        await DeviceTimelineHandler.HandleAsync(evt, writer, NullLogger<DeviceTimelineHandlerCategory>.Instance, TestContext.Current.CancellationToken);

        await writer.Received(1).PostEntryAsync(
            "Device",
            DeviceIdN,
            TimelineEntryType.SystemLog,
            Arg.Is<string>(s => s.Contains("provisioned", StringComparison.Ordinal) && s.Contains("SN-001", StringComparison.Ordinal)),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Activated_WritesSystemLog_ProvisioningToActive()
    {
        ITimelineWriter writer = Substitute.For<ITimelineWriter>();
        var evt = new DeviceActivatedEvent(DeviceId, "SN-002", TenantId: null);

        await DeviceTimelineHandler.HandleAsync(evt, writer, NullLogger<DeviceTimelineHandlerCategory>.Instance, TestContext.Current.CancellationToken);

        await writer.Received(1).PostEntryAsync(
            "Device", DeviceIdN, TimelineEntryType.SystemLog,
            Arg.Is<string>(s => s.Contains("activated", StringComparison.Ordinal) && s.Contains("Active", StringComparison.Ordinal)),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Suspended_WritesSystemLog_WithReason()
    {
        ITimelineWriter writer = Substitute.For<ITimelineWriter>();
        var evt = new DeviceSuspendedEvent(DeviceId, Reason: "Maintenance window", TenantId: null);

        await DeviceTimelineHandler.HandleAsync(evt, writer, NullLogger<DeviceTimelineHandlerCategory>.Instance, TestContext.Current.CancellationToken);

        await writer.Received(1).PostEntryAsync(
            "Device", DeviceIdN, TimelineEntryType.SystemLog,
            Arg.Is<string>(s => s.Contains("suspended", StringComparison.Ordinal) && s.Contains("Maintenance window", StringComparison.Ordinal)),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reactivated_WritesSystemLog_SuspendedToActive()
    {
        ITimelineWriter writer = Substitute.For<ITimelineWriter>();
        var evt = new DeviceReactivatedEvent(DeviceId, "SN-003", TenantId: null);

        await DeviceTimelineHandler.HandleAsync(evt, writer, NullLogger<DeviceTimelineHandlerCategory>.Instance, TestContext.Current.CancellationToken);

        await writer.Received(1).PostEntryAsync(
            "Device", DeviceIdN, TimelineEntryType.SystemLog,
            Arg.Is<string>(s => s.Contains("reactivated", StringComparison.Ordinal) && s.Contains("Suspended", StringComparison.Ordinal)),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Decommissioned_WritesSystemLog()
    {
        ITimelineWriter writer = Substitute.For<ITimelineWriter>();
        var evt = new DeviceDecommissionedEvent(DeviceId, TenantId: null);

        await DeviceTimelineHandler.HandleAsync(evt, writer, NullLogger<DeviceTimelineHandlerCategory>.Instance, TestContext.Current.CancellationToken);

        await writer.Received(1).PostEntryAsync(
            "Device", DeviceIdN, TimelineEntryType.SystemLog,
            Arg.Is<string>(s => s.Contains("decommissioned", StringComparison.Ordinal)),
            null,
            Arg.Any<CancellationToken>());
    }
}
