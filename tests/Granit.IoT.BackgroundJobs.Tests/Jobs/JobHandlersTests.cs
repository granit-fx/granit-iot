using Granit.Events;
using Granit.IoT.Abstractions;
using Granit.IoT.BackgroundJobs.Internal;
using Granit.IoT.BackgroundJobs.Jobs;
using Granit.IoT.BackgroundJobs.Services;
using Granit.IoT.Diagnostics;
using Granit.MultiTenancy;
using Granit.Settings.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.BackgroundJobs.Tests.Jobs;

public sealed class JobHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DeviceHeartbeatTimeoutHandler_DelegatesToService()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid?>());

        DeviceHeartbeatTimeoutService service = new(
            reader,
            Substitute.For<ISettingProvider>(),
            StubTenant(),
            Substitute.For<IDistributedEventBus>(),
            new DeviceOfflineTrackerCache(new MemoryCache(new MemoryCacheOptions())),
            new IoTMetrics(new TestMeterFactory()),
            new FakeTimeProvider(Now),
            NullLogger<DeviceHeartbeatTimeoutService>.Instance);

        await DeviceHeartbeatTimeoutHandler.HandleAsync(
            new DeviceHeartbeatTimeoutJob(),
            service,
            TestContext.Current.CancellationToken);

        await reader.Received(1).GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StaleTelemetryPurgeHandler_DelegatesToService()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid?>());
        ITelemetryPurger purger = Substitute.For<ITelemetryPurger>();

        StaleTelemetryPurgeService service = new(
            reader,
            purger,
            Substitute.For<ISettingProvider>(),
            StubTenant(),
            new IoTMetrics(new TestMeterFactory()),
            new FakeTimeProvider(Now),
            NullLogger<StaleTelemetryPurgeService>.Instance);

        await StaleTelemetryPurgeHandler.HandleAsync(
            new StaleTelemetryPurgeJob(),
            service,
            TestContext.Current.CancellationToken);

        await reader.Received(1).GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TelemetryPartitionMaintenanceHandler_DelegatesToService()
    {
        ITelemetryPartitionMaintainer maintainer = new NoOpTelemetryPartitionMaintainer();

        TelemetryPartitionMaintenanceService service = new(
            maintainer,
            new IoTMetrics(new TestMeterFactory()),
            new FakeTimeProvider(Now),
            NullLogger<TelemetryPartitionMaintenanceService>.Instance);

        await TelemetryPartitionMaintenanceHandler.HandleAsync(
            new TelemetryPartitionMaintenanceJob(),
            service,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NoOpTelemetryPartitionMaintainer_ReportsNotPartitioned()
    {
        NoOpTelemetryPartitionMaintainer maintainer = new();

        bool isPartitioned = await maintainer.IsParentPartitionedAsync(TestContext.Current.CancellationToken);

        isPartitioned.ShouldBeFalse();
    }

    [Fact]
    public async Task NoOpTelemetryPartitionMaintainer_CreatePartition_IsNoOp()
    {
        NoOpTelemetryPartitionMaintainer maintainer = new();

        await maintainer.CreatePartitionAsync(2026, 4, TestContext.Current.CancellationToken);
    }

    private static ICurrentTenant StubTenant()
    {
        ICurrentTenant t = Substitute.For<ICurrentTenant>();
        t.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
        return t;
    }
}
