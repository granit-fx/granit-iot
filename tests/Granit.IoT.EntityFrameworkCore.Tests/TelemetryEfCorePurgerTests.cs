using Granit.IoT.Domain;
using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.MultiTenancy;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Tests;

public sealed class TelemetryEfCorePurgerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = TestDbContextFactory.Create();
    private readonly TelemetryEfCorePurger _purger;
    private readonly TelemetryEfCoreWriter _writer;
    private readonly DeviceEfCoreWriter _deviceWriter;

    public TelemetryEfCorePurgerTests()
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        _purger = new TelemetryEfCorePurger(_factory);
        _writer = new TelemetryEfCoreWriter(_factory, currentTenant);
        _deviceWriter = new DeviceEfCoreWriter(_factory, currentTenant);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task PurgeOlderThanAsync_DeletesOnlyRowsBelowCutoffForGivenTenants()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        Guid deviceA = await SeedDeviceAsync("DEV-A", tenantA);
        Guid deviceB = await SeedDeviceAsync("DEV-B", tenantB);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        await SeedPointAsync(deviceA, tenantA, now.AddDays(-200));
        await SeedPointAsync(deviceA, tenantA, now.AddDays(-50));
        await SeedPointAsync(deviceB, tenantB, now.AddDays(-200));

        long deleted = await _purger.PurgeOlderThanAsync(
            [tenantA], now.AddDays(-100), TestContext.Current.CancellationToken);

        deleted.ShouldBe(1);
    }

    [Fact]
    public async Task PurgeOlderThanAsync_EmptyTenantList_ReturnsZeroAndNoSql()
    {
        long deleted = await _purger.PurgeOlderThanAsync(
            [], DateTimeOffset.UtcNow, TestContext.Current.CancellationToken);

        deleted.ShouldBe(0);
    }

    [Fact]
    public async Task PurgeOlderThanAsync_BulkDeletesAcrossMultipleTenantsInOneCall()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        Guid deviceA = await SeedDeviceAsync("BULK-A", tenantA);
        Guid deviceB = await SeedDeviceAsync("BULK-B", tenantB);

        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-100);
        await SeedPointAsync(deviceA, tenantA, cutoff.AddDays(-1));
        await SeedPointAsync(deviceB, tenantB, cutoff.AddDays(-1));

        long deleted = await _purger.PurgeOlderThanAsync(
            [tenantA, tenantB], cutoff, TestContext.Current.CancellationToken);

        deleted.ShouldBe(2);
    }

    private async Task<Guid> SeedDeviceAsync(string serial, Guid? tenantId)
    {
        Device device = Device.Create(
            Guid.NewGuid(),
            tenantId: tenantId,
            DeviceSerialNumber.Create(serial),
            HardwareModel.Create("M"),
            FirmwareVersion.Create("1.0.0"));
        await _deviceWriter.AddAsync(device, TestContext.Current.CancellationToken);
        return device.Id;
    }

    private Task SeedPointAsync(Guid deviceId, Guid? tenantId, DateTimeOffset recordedAt) =>
        _writer.AppendAsync(
            TelemetryPoint.Create(
                Guid.NewGuid(), deviceId, tenantId, recordedAt,
                new Dictionary<string, double> { ["t"] = 1.0 }),
            TestContext.Current.CancellationToken);
}
