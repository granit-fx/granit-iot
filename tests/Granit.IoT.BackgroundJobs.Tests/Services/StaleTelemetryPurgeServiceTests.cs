using Granit.IoT.Abstractions;
using Granit.IoT.BackgroundJobs.Services;
using Granit.IoT.Diagnostics;
using Granit.IoT.Notifications;
using Granit.MultiTenancy;
using Granit.Settings.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.BackgroundJobs.Tests.Services;

public sealed class StaleTelemetryPurgeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 17, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_NoTenants_ReturnsWithoutCallingPurger()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid?>());
        ITelemetryPurger purger = Substitute.For<ITelemetryPurger>();

        StaleTelemetryPurgeService service = CreateService(reader, purger, Substitute.For<ISettingProvider>());

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        await purger.DidNotReceive().PurgeOlderThanAsync(
            Arg.Any<IReadOnlyCollection<Guid?>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_BucketsTenantsByRetentionValue_OneSqlCallPerBucket()
    {
        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var t3 = Guid.NewGuid();

        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>())
            .Returns([t1, t2, t3]);

        ISettingProvider settings = Substitute.For<ISettingProvider>();
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
        // Sequence answers in the order tenants are iterated: t1=365, t2=90, t3=365.
        var answers = new Queue<string?>(["365", "90", "365"]);
        settings.GetOrNullAsync(IoTSettingNames.TelemetryRetentionDays, Arg.Any<CancellationToken>())
            .Returns(_ => answers.Dequeue());

        ITelemetryPurger purger = Substitute.For<ITelemetryPurger>();
        purger.PurgeOlderThanAsync(Arg.Any<IReadOnlyCollection<Guid?>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);

        StaleTelemetryPurgeService service = CreateService(reader, purger, settings, currentTenant);

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        // Two distinct retention values (365 + 90) → exactly two SQL deletes.
        await purger.Received(2).PurgeOlderThanAsync(
            Arg.Any<IReadOnlyCollection<Guid?>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());

        // 365-day bucket got t1+t3 (2 tenants), cutoff = Now - 365 days.
        await purger.Received(1).PurgeOlderThanAsync(
            Arg.Is<IReadOnlyCollection<Guid?>>(c => c.Count == 2 && c.Contains((Guid?)t1) && c.Contains((Guid?)t3)),
            Now.AddDays(-365),
            Arg.Any<CancellationToken>());

        // 90-day bucket got t2 alone, cutoff = Now - 90 days.
        await purger.Received(1).PurgeOlderThanAsync(
            Arg.Is<IReadOnlyCollection<Guid?>>(c => c.Count == 1 && c.Contains((Guid?)t2)),
            Now.AddDays(-90),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MissingSetting_FallsBackToDefault365()
    {
        var tenant = Guid.NewGuid();
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.GetDistinctTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([(Guid?)tenant]);

        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(IoTSettingNames.TelemetryRetentionDays, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        ITelemetryPurger purger = Substitute.For<ITelemetryPurger>();
        purger.PurgeOlderThanAsync(Arg.Any<IReadOnlyCollection<Guid?>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);

        StaleTelemetryPurgeService service = CreateService(reader, purger, settings);

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        await purger.Received(1).PurgeOlderThanAsync(
            Arg.Any<IReadOnlyCollection<Guid?>>(),
            Now.AddDays(-StaleTelemetryPurgeService.DefaultRetentionDays),
            Arg.Any<CancellationToken>());
    }

    private static StaleTelemetryPurgeService CreateService(
        IDeviceReader reader,
        ITelemetryPurger purger,
        ISettingProvider settings,
        ICurrentTenant? currentTenant = null) =>
        new(
            reader,
            purger,
            settings,
            currentTenant ?? StubTenant(),
            new IoTMetrics(new TestMeterFactory()),
            new FakeTimeProvider(Now),
            NullLogger<StaleTelemetryPurgeService>.Instance);

    private static ICurrentTenant StubTenant()
    {
        ICurrentTenant t = Substitute.For<ICurrentTenant>();
        t.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
        return t;
    }
}
