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

    // ===== HELPERS =====

    private static Device CreateDevice(string serialNumber = "SN-TEST-001") =>
        Device.Create(
            Guid.NewGuid(),
            tenantId: null,
            DeviceSerialNumber.Create(serialNumber),
            HardwareModel.Create("TestSensor-V1"),
            FirmwareVersion.Create("1.0.0"),
            label: "Test Device");
}
