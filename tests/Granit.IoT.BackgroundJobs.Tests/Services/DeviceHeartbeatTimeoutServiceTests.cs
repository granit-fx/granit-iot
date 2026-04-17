using Granit.Events;
using Granit.IoT.Abstractions;
using Granit.IoT.BackgroundJobs.Internal;
using Granit.IoT.BackgroundJobs.Services;
using Granit.IoT.Diagnostics;
using Granit.IoT.Domain;
using Granit.IoT.Events;
using Granit.IoT.Notifications;
using Granit.MultiTenancy;
using Granit.Settings.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.BackgroundJobs.Tests.Services;

public sealed class DeviceHeartbeatTimeoutServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_StaleDevice_PublishesEtoAndAddsToTracker()
    {
        var tenant = Guid.NewGuid();
        Device stale = BuildDevice(tenant, "STALE-1", Now.AddMinutes(-30));

        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([(Guid?)tenant]);
        reader.FindStaleAsync(Arg.Any<IReadOnlyCollection<Guid?>>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([stale]);

        ISettingProvider settings = StubTimeout(minutes: 15);
        IDistributedEventBus bus = Substitute.For<IDistributedEventBus>();
        var tracker = new DeviceOfflineTrackerCache(new MemoryCache(new MemoryCacheOptions()));

        DeviceHeartbeatTimeoutService service = CreateService(reader, settings, bus, tracker);

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        await bus.Received(1).PublishAsync(
            Arg.Is<DeviceOfflineDetectedEto>(e => e.DeviceId == stale.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SameDeviceTwice_SecondRunIsSuppressed()
    {
        var tenant = Guid.NewGuid();
        Device stale = BuildDevice(tenant, "FLAPPY-1", Now.AddMinutes(-30));

        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([(Guid?)tenant]);
        reader.FindStaleAsync(Arg.Any<IReadOnlyCollection<Guid?>>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([stale]);

        ISettingProvider settings = StubTimeout(minutes: 15);
        IDistributedEventBus bus = Substitute.For<IDistributedEventBus>();
        var tracker = new DeviceOfflineTrackerCache(new MemoryCache(new MemoryCacheOptions()));

        DeviceHeartbeatTimeoutService service = CreateService(reader, settings, bus, tracker);

        await service.ExecuteAsync(TestContext.Current.CancellationToken);
        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        // Still one publish — the second run found the device tracked and suppressed it.
        await bus.Received(1).PublishAsync(
            Arg.Any<DeviceOfflineDetectedEto>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_TenantWithZeroTimeout_IsSkipped()
    {
        var tenant = Guid.NewGuid();

        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([(Guid?)tenant]);

        ISettingProvider settings = StubTimeout(minutes: 0);
        IDistributedEventBus bus = Substitute.For<IDistributedEventBus>();

        DeviceHeartbeatTimeoutService service = CreateService(reader, settings, bus);

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        await reader.DidNotReceive().FindStaleAsync(
            Arg.Any<IReadOnlyCollection<Guid?>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await bus.DidNotReceive().PublishAsync(Arg.Any<DeviceOfflineDetectedEto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_TwoDistinctTimeouts_IssuesExactlyTwoQueries()
    {
        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var t3 = Guid.NewGuid();

        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([t1, t2, t3]);
        reader.FindStaleAsync(Arg.Any<IReadOnlyCollection<Guid?>>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Device>());

        ISettingProvider settings = Substitute.For<ISettingProvider>();
        var answers = new Queue<string?>(["15", "30", "15"]);
        settings.GetOrNullAsync(IoTSettingNames.HeartbeatTimeoutMinutes, Arg.Any<CancellationToken>())
            .Returns(_ => answers.Dequeue());
        settings.GetOrNullAsync(IoTSettingNames.HeartbeatOfflineNotificationCacheMinutes, Arg.Any<CancellationToken>())
            .Returns("60");

        IDistributedEventBus bus = Substitute.For<IDistributedEventBus>();
        DeviceHeartbeatTimeoutService service = CreateService(reader, settings, bus);

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        await reader.Received(2).FindStaleAsync(
            Arg.Any<IReadOnlyCollection<Guid?>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    private static Device BuildDevice(Guid tenantId, string serial, DateTimeOffset lastHeartbeatAt)
    {
        var device = Device.Create(
            Guid.NewGuid(), tenantId,
            DeviceSerialNumber.Create(serial),
            HardwareModel.Create("Sensor"),
            FirmwareVersion.Create("1.0.0"));
        device.Activate();
        device.RecordHeartbeat(lastHeartbeatAt);
        return device;
    }

    private static ISettingProvider StubTimeout(int minutes)
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(IoTSettingNames.HeartbeatTimeoutMinutes, Arg.Any<CancellationToken>())
            .Returns(minutes.ToString(System.Globalization.CultureInfo.InvariantCulture));
        settings.GetOrNullAsync(IoTSettingNames.HeartbeatOfflineNotificationCacheMinutes, Arg.Any<CancellationToken>())
            .Returns("60");
        return settings;
    }

    private static DeviceHeartbeatTimeoutService CreateService(
        IDeviceReader reader,
        ISettingProvider settings,
        IDistributedEventBus bus,
        DeviceOfflineTrackerCache? tracker = null)
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());

        return new DeviceHeartbeatTimeoutService(
            reader,
            settings,
            currentTenant,
            bus,
            tracker ?? new DeviceOfflineTrackerCache(new MemoryCache(new MemoryCacheOptions())),
            new IoTMetrics(new TestMeterFactory()),
            new FakeTimeProvider(Now),
            NullLogger<DeviceHeartbeatTimeoutService>.Instance);
    }
}
