using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.Mcp.Responses;
using Granit.IoT.Mcp.Tools;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Mcp.Tests.Tools;

public class DeviceMcpToolsTests
{
    [Fact]
    public async Task ListAsync_NoStatusFilter_ReturnsMappedDevices()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        Device device = BuildDevice("SN-001");
        reader.ListAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new[] { device });

        IReadOnlyList<DeviceMcpResponse> result = await DeviceMcpTools.ListAsync(
            reader, statusFilter: null, cancellationToken: TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].SerialNumber.ShouldBe("SN-001");
        result[0].Status.ShouldBe("Provisioning");
    }

    [Fact]
    public async Task ListAsync_ParsesStatusFilterCaseInsensitive()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.ListAsync(DeviceStatus.Suspended, 1, 20, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Device>());

        await DeviceMcpTools.ListAsync(
            reader, statusFilter: "suspended", cancellationToken: TestContext.Current.CancellationToken);

        await reader.Received(1).ListAsync(DeviceStatus.Suspended, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_InvalidStatusFilter_TreatedAsNoFilter()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.ListAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Device>());

        await DeviceMcpTools.ListAsync(
            reader, statusFilter: "bogus", cancellationToken: TestContext.Current.CancellationToken);

        await reader.Received(1).ListAsync(null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_CapsPageSizeAt100AndNormalizesPage()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.ListAsync(null, 1, 100, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Device>());

        await DeviceMcpTools.ListAsync(
            reader, statusFilter: null, page: 0, pageSize: 500,
            cancellationToken: TestContext.Current.CancellationToken);

        await reader.Received(1).ListAsync(null, 1, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_UnknownDevice_ReturnsNull()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        var deviceId = Guid.NewGuid();
        reader.FindAsync(deviceId, Arg.Any<CancellationToken>()).Returns((Device?)null);

        DeviceMcpResponse? result = await DeviceMcpTools.GetAsync(
            reader, deviceId, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_Found_ReturnsMappedResponseWithHeartbeat()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        Device device = BuildDevice("SN-042");
        DateTimeOffset heartbeat = new(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);
        device.RecordHeartbeat(heartbeat);
        reader.FindAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);

        DeviceMcpResponse? result = await DeviceMcpTools.GetAsync(
            reader, device.Id, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(device.Id);
        result.SerialNumber.ShouldBe("SN-042");
        result.LastSeenAt.ShouldBe(heartbeat);
    }

    private static Device BuildDevice(string serial) => Device.Create(
        id: Guid.NewGuid(),
        tenantId: Guid.NewGuid(),
        serialNumber: DeviceSerialNumber.Create(serial),
        model: HardwareModel.Create("cold-chain-v2"),
        firmware: FirmwareVersion.Create("1.0.0"),
        label: "fridge-1");
}
