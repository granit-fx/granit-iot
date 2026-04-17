using Granit.IoT.Domain;
using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.MultiTenancy;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Tests;

public sealed class DeviceEfCoreStoreTests : IDisposable
{
    private readonly TestDbContextFactory _factory = TestDbContextFactory.Create();
    private readonly DeviceEfCoreReader _reader;
    private readonly DeviceEfCoreWriter _writer;

    public DeviceEfCoreStoreTests()
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        _reader = new DeviceEfCoreReader(_factory, currentTenant);
        _writer = new DeviceEfCoreWriter(_factory, currentTenant);
    }

    public void Dispose() => _factory.Dispose();

    // ===== INSERT & GET =====

    [Fact]
    public async Task AddAsync_ThenFindAsync_ReturnsDevice()
    {
        Device device = CreateDevice();

        await _writer.AddAsync(device, TestContext.Current.CancellationToken);

        Device? result = await _reader.FindAsync(device.Id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(device.Id);
        result.SerialNumber.Value.ShouldBe(device.SerialNumber.Value);
        result.Model.Value.ShouldBe(device.Model.Value);
        result.Firmware.Value.ShouldBe(device.Firmware.Value);
        result.Status.ShouldBe(DeviceStatus.Provisioning);
    }

    [Fact]
    public async Task FindAsync_NonExistentId_ReturnsNull()
    {
        Device? result = await _reader.FindAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    // ===== FIND BY SERIAL NUMBER =====

    [Fact]
    public async Task FindBySerialNumberAsync_ExistingSerial_ReturnsDevice()
    {
        Device device = CreateDevice(serialNumber: "FIND-BY-SN-001");
        await _writer.AddAsync(device, TestContext.Current.CancellationToken);

        Device? result = await _reader.FindBySerialNumberAsync("FIND-BY-SN-001", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(device.Id);
    }

    [Fact]
    public async Task FindBySerialNumberAsync_NonExistent_ReturnsNull()
    {
        Device? result = await _reader.FindBySerialNumberAsync("NONEXISTENT", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    // ===== LIST & COUNT =====

    [Fact]
    public async Task ListAsync_ReturnsAllDevices()
    {
        await _writer.AddAsync(CreateDevice(serialNumber: "LIST-001"), TestContext.Current.CancellationToken);
        await _writer.AddAsync(CreateDevice(serialNumber: "LIST-002"), TestContext.Current.CancellationToken);
        await _writer.AddAsync(CreateDevice(serialNumber: "LIST-003"), TestContext.Current.CancellationToken);

        IReadOnlyList<Device> result = await _reader.ListAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ListAsync_FilterByStatus_ReturnsFiltered()
    {
        Device active = CreateDevice(serialNumber: "STATUS-ACTIVE");
        active.Activate();
        await _writer.AddAsync(active, TestContext.Current.CancellationToken);

        await _writer.AddAsync(CreateDevice(serialNumber: "STATUS-PROV"), TestContext.Current.CancellationToken);

        IReadOnlyList<Device> result = await _reader.ListAsync(
            DeviceStatus.Active,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldAllBe(d => d.Status == DeviceStatus.Active);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        await _writer.AddAsync(CreateDevice(serialNumber: "COUNT-001"), TestContext.Current.CancellationToken);
        await _writer.AddAsync(CreateDevice(serialNumber: "COUNT-002"), TestContext.Current.CancellationToken);

        int count = await _reader.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        count.ShouldBeGreaterThanOrEqualTo(2);
    }

    // ===== EXISTS =====

    [Fact]
    public async Task ExistsAsync_ExistingSerial_ReturnsTrue()
    {
        await _writer.AddAsync(CreateDevice(serialNumber: "EXISTS-001"), TestContext.Current.CancellationToken);

        bool exists = await _reader.ExistsAsync("EXISTS-001", TestContext.Current.CancellationToken);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistent_ReturnsFalse()
    {
        bool exists = await _reader.ExistsAsync("DOES-NOT-EXIST", TestContext.Current.CancellationToken);

        exists.ShouldBeFalse();
    }

    // ===== UPDATE =====

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        Device device = CreateDevice(serialNumber: "UPDATE-001");
        await _writer.AddAsync(device, TestContext.Current.CancellationToken);

        device.UpdateFirmware(FirmwareVersion.Create("2.0.0"));
        device.UpdateLabel("Updated Label");
        await _writer.UpdateAsync(device, TestContext.Current.CancellationToken);

        Device? result = await _reader.FindAsync(device.Id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Firmware.Value.ShouldBe("2.0.0");
        result.Label.ShouldBe("Updated Label");
    }

    // ===== HEARTBEAT (ExecuteUpdateAsync) =====

    [Fact]
    public async Task UpdateHeartbeatAsync_SetsLastHeartbeatAt()
    {
        Device device = CreateDevice(serialNumber: "HEARTBEAT-001");
        await _writer.AddAsync(device, TestContext.Current.CancellationToken);

        DateTimeOffset heartbeatAt = DateTimeOffset.UtcNow;
        await _writer.UpdateHeartbeatAsync(device.Id, heartbeatAt, TestContext.Current.CancellationToken);

        Device? result = await _reader.FindAsync(device.Id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.LastHeartbeatAt.ShouldNotBeNull();
    }

    // ===== SOFT DELETE =====

    [Fact]
    public async Task DeleteAsync_SoftDeletes_DeviceNotFoundAfter()
    {
        Device device = CreateDevice(serialNumber: "DELETE-001");
        await _writer.AddAsync(device, TestContext.Current.CancellationToken);

        await _writer.DeleteAsync(device, TestContext.Current.CancellationToken);

        Device? result = await _reader.FindAsync(device.Id, TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    // ===== GetDistinctTenantIdsAsync =====

    [Fact]
    public async Task GetDistinctTenantIdsAsync_ReturnsDistinctTenants()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await _writer.AddAsync(CreateDevice("TENANT-A-1", tenantId: tenantA), TestContext.Current.CancellationToken);
        await _writer.AddAsync(CreateDevice("TENANT-A-2", tenantId: tenantA), TestContext.Current.CancellationToken);
        await _writer.AddAsync(CreateDevice("TENANT-B-1", tenantId: tenantB), TestContext.Current.CancellationToken);
        await _writer.AddAsync(CreateDevice("GLOBAL-1", tenantId: null), TestContext.Current.CancellationToken);

        IReadOnlyList<Guid?> tenants =
            await _reader.GetDistinctTenantIdsAsync(TestContext.Current.CancellationToken);

        tenants.ShouldContain(tenantA);
        tenants.ShouldContain(tenantB);
        tenants.ShouldContain((Guid?)null);
        tenants.Count.ShouldBe(3);
    }

    // ===== FindStaleAsync =====

    [Fact]
    public async Task FindStaleAsync_ReturnsActiveDevicesWithStaleHeartbeat()
    {
        var tenant = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Device stale = CreateDevice("STALE-001", tenantId: tenant);
        stale.Activate();
        stale.RecordHeartbeat(now.AddMinutes(-30));
        await _writer.AddAsync(stale, TestContext.Current.CancellationToken);

        Device fresh = CreateDevice("FRESH-001", tenantId: tenant);
        fresh.Activate();
        fresh.RecordHeartbeat(now.AddMinutes(-1));
        await _writer.AddAsync(fresh, TestContext.Current.CancellationToken);

        IReadOnlyList<Device> result = await _reader.FindStaleAsync(
            [tenant], now.AddMinutes(-15), batchSize: 100, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].SerialNumber.Value.ShouldBe("STALE-001");
    }

    [Fact]
    public async Task FindStaleAsync_TreatsNullHeartbeatAsStale()
    {
        var tenant = Guid.NewGuid();
        Device device = CreateDevice("NEVER-SEEN-001", tenantId: tenant);
        device.Activate();
        await _writer.AddAsync(device, TestContext.Current.CancellationToken);

        IReadOnlyList<Device> result = await _reader.FindStaleAsync(
            [tenant], DateTimeOffset.UtcNow, batchSize: 100, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FindStaleAsync_SkipsNonActiveDevices()
    {
        var tenant = Guid.NewGuid();
        Device provisioning = CreateDevice("PROV-001", tenantId: tenant);
        await _writer.AddAsync(provisioning, TestContext.Current.CancellationToken);

        Device decommissioned = CreateDevice("DECOM-001", tenantId: tenant);
        decommissioned.Decommission();
        await _writer.AddAsync(decommissioned, TestContext.Current.CancellationToken);

        IReadOnlyList<Device> result = await _reader.FindStaleAsync(
            [tenant], DateTimeOffset.UtcNow, batchSize: 100, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindStaleAsync_FiltersByTenantList()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        Device deviceA = CreateDevice("TENANT-A-STALE", tenantId: tenantA);
        deviceA.Activate();
        await _writer.AddAsync(deviceA, TestContext.Current.CancellationToken);

        Device deviceB = CreateDevice("TENANT-B-STALE", tenantId: tenantB);
        deviceB.Activate();
        await _writer.AddAsync(deviceB, TestContext.Current.CancellationToken);

        IReadOnlyList<Device> result = await _reader.FindStaleAsync(
            [tenantA], DateTimeOffset.UtcNow, batchSize: 100, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].SerialNumber.Value.ShouldBe("TENANT-A-STALE");
    }

    [Fact]
    public async Task FindStaleAsync_RespectsBatchSize()
    {
        var tenant = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            Device d = CreateDevice($"BATCH-{i:D3}", tenantId: tenant);
            d.Activate();
            await _writer.AddAsync(d, TestContext.Current.CancellationToken);
        }

        IReadOnlyList<Device> result = await _reader.FindStaleAsync(
            [tenant], DateTimeOffset.UtcNow, batchSize: 2, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
    }

    // ===== HELPERS =====

    private static Device CreateDevice(string serialNumber = "SN-TEST-001", Guid? tenantId = null) =>
        Device.Create(
            Guid.NewGuid(),
            tenantId: tenantId,
            DeviceSerialNumber.Create(serialNumber),
            HardwareModel.Create("TestSensor-V1"),
            FirmwareVersion.Create("1.0.0"),
            label: "Test Device");
}
