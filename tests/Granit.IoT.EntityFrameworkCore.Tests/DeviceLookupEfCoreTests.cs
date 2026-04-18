using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.MultiTenancy;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Tests;

public sealed class DeviceLookupEfCoreTests : IDisposable
{
    private readonly TestDbContextFactory _factory = TestDbContextFactory.Create();
    private readonly DeviceEfCoreWriter _writer;
    private readonly DeviceLookupEfCore _lookup;

    public DeviceLookupEfCoreTests()
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        _writer = new DeviceEfCoreWriter(_factory, currentTenant);
        _lookup = new DeviceLookupEfCore(_factory, dataFilter: null);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task FindBySerialNumberAsync_NullOrEmpty_Throws()
    {
        await Should.ThrowAsync<ArgumentException>(() =>
            _lookup.FindBySerialNumberAsync(string.Empty, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindBySerialNumberAsync_NotFound_ReturnsNull()
    {
        DeviceLookupResult? result = await _lookup.FindBySerialNumberAsync("MISSING", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindBySerialNumberAsync_Found_ReturnsLookupResult()
    {
        var d = Device.Create(
            Guid.NewGuid(), tenantId: null,
            DeviceSerialNumber.Create("SN-LOOKUP"),
            HardwareModel.Create("Model"),
            FirmwareVersion.Create("1.0.0"));
        await _writer.AddAsync(d, TestContext.Current.CancellationToken);

        DeviceLookupResult? result = await _lookup.FindBySerialNumberAsync("SN-LOOKUP", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.DeviceId.ShouldBe(d.Id);
    }
}
