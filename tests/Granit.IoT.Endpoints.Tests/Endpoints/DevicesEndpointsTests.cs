using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.Endpoints.Dtos;
using Granit.IoT.Endpoints.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Endpoints.Tests.Endpoints;

public sealed class DevicesEndpointsTests
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid GeneratedId = Guid.NewGuid();

    [Fact]
    public async Task ListDevicesAsync_ReturnsDeviceResponses()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        Device d = NewActiveDevice("SN-1");
        reader.ListAsync(Arg.Any<DeviceStatus?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([d]);

        Ok<IReadOnlyList<DeviceResponse>> result = await DevicesEndpoints
            .ListDevicesAsync(reader, status: null, page: 1, pageSize: 20, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].SerialNumber.ShouldBe("SN-1");
    }

    [Fact]
    public async Task GetDeviceByIdAsync_NotFound_Returns404()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        reader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns((Device?)null);

        Results<Ok<DeviceResponse>, NotFound> result = await DevicesEndpoints
            .GetDeviceByIdAsync(DeviceId, reader, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetDeviceByIdAsync_Found_ReturnsOk()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        Device d = NewActiveDevice("SN-2");
        reader.FindAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);

        Results<Ok<DeviceResponse>, NotFound> result = await DevicesEndpoints
            .GetDeviceByIdAsync(d.Id, reader, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Ok<DeviceResponse> ok = result.Result.ShouldBeOfType<Ok<DeviceResponse>>();
        ok.Value!.SerialNumber.ShouldBe("SN-2");
    }

    [Fact]
    public async Task ProvisionDeviceAsync_DuplicateSerial_Returns409()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        IDeviceWriter writer = Substitute.For<IDeviceWriter>();
        IGuidGenerator gen = Substitute.For<IGuidGenerator>();
        gen.Create().Returns(GeneratedId);
        reader.ExistsAsync("SN-DUP", Arg.Any<CancellationToken>()).Returns(true);

        DeviceProvisionRequest req = new()
        {
            SerialNumber = "SN-DUP",
            HardwareModel = "Model-1",
            FirmwareVersion = "1.0.0",
            Label = "label",
        };

        Results<Created<DeviceResponse>, ProblemHttpResult> result = await DevicesEndpoints
            .ProvisionDeviceAsync(req, reader, writer, gen, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProblemHttpResult prob = result.Result.ShouldBeOfType<ProblemHttpResult>();
        prob.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        await writer.DidNotReceiveWithAnyArgs().AddAsync(default!, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ProvisionDeviceAsync_New_Returns201AndPersists()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        IDeviceWriter writer = Substitute.For<IDeviceWriter>();
        IGuidGenerator gen = Substitute.For<IGuidGenerator>();
        gen.Create().Returns(GeneratedId);
        reader.ExistsAsync("SN-NEW", Arg.Any<CancellationToken>()).Returns(false);

        DeviceProvisionRequest req = new()
        {
            SerialNumber = "SN-NEW",
            HardwareModel = "Model-1",
            FirmwareVersion = "1.0.0",
            Label = "label",
        };

        Results<Created<DeviceResponse>, ProblemHttpResult> result = await DevicesEndpoints
            .ProvisionDeviceAsync(req, reader, writer, gen, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Created<DeviceResponse> created = result.Result.ShouldBeOfType<Created<DeviceResponse>>();
        created.Value!.Id.ShouldBe(GeneratedId);
        created.Value.SerialNumber.ShouldBe("SN-NEW");
        await writer.Received(1).AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task UpdateDeviceAsync_NotFound_Returns404()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        IDeviceWriter writer = Substitute.For<IDeviceWriter>();
        reader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns((Device?)null);

        DeviceUpdateRequest req = new() { FirmwareVersion = "2.0.0", Label = "x" };

        Results<Ok<DeviceResponse>, NotFound> result = await DevicesEndpoints
            .UpdateDeviceAsync(DeviceId, req, reader, writer, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NotFound>();
        await writer.DidNotReceiveWithAnyArgs().UpdateAsync(default!, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task UpdateDeviceAsync_AppliesFirmwareAndLabel()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        IDeviceWriter writer = Substitute.For<IDeviceWriter>();
        Device d = NewActiveDevice("SN-3");
        reader.FindAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);

        DeviceUpdateRequest req = new() { FirmwareVersion = "2.0.0", Label = "renamed" };

        Results<Ok<DeviceResponse>, NotFound> result = await DevicesEndpoints
            .UpdateDeviceAsync(d.Id, req, reader, writer, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Ok<DeviceResponse> ok = result.Result.ShouldBeOfType<Ok<DeviceResponse>>();
        ok.Value!.FirmwareVersion.ShouldBe("2.0.0");
        ok.Value.Label.ShouldBe("renamed");
        await writer.Received(1).UpdateAsync(d, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task UpdateDeviceAsync_NoChangesRequested_StillUpdates()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        IDeviceWriter writer = Substitute.For<IDeviceWriter>();
        Device d = NewActiveDevice("SN-4");
        reader.FindAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);

        DeviceUpdateRequest req = new() { FirmwareVersion = null, Label = null };

        Results<Ok<DeviceResponse>, NotFound> result = await DevicesEndpoints
            .UpdateDeviceAsync(d.Id, req, reader, writer, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<Ok<DeviceResponse>>();
        await writer.Received(1).UpdateAsync(d, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task DecommissionDeviceAsync_NotFound_Returns404()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        IDeviceWriter writer = Substitute.For<IDeviceWriter>();
        reader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns((Device?)null);

        Results<NoContent, NotFound, ProblemHttpResult> result = await DevicesEndpoints
            .DecommissionDeviceAsync(DeviceId, reader, writer, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task DecommissionDeviceAsync_ActiveDevice_Returns409()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        IDeviceWriter writer = Substitute.For<IDeviceWriter>();
        Device d = NewActiveDevice("SN-5");
        reader.FindAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);

        Results<NoContent, NotFound, ProblemHttpResult> result = await DevicesEndpoints
            .DecommissionDeviceAsync(d.Id, reader, writer, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProblemHttpResult prob = result.Result.ShouldBeOfType<ProblemHttpResult>();
        prob.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        await writer.DidNotReceiveWithAnyArgs().DeleteAsync(default!, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task DecommissionDeviceAsync_ProvisioningDevice_Returns204AndDeletes()
    {
        IDeviceReader reader = Substitute.For<IDeviceReader>();
        IDeviceWriter writer = Substitute.For<IDeviceWriter>();
        var d = Device.Create(
            Guid.NewGuid(), tenantId: null,
            DeviceSerialNumber.Create("SN-6"),
            HardwareModel.Create("Model"),
            FirmwareVersion.Create("1.0.0"));
        reader.FindAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);

        Results<NoContent, NotFound, ProblemHttpResult> result = await DevicesEndpoints
            .DecommissionDeviceAsync(d.Id, reader, writer, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NoContent>();
        await writer.Received(1).DeleteAsync(d, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static Device NewActiveDevice(string serial)
    {
        var d = Device.Create(
            Guid.NewGuid(), tenantId: null,
            DeviceSerialNumber.Create(serial),
            HardwareModel.Create("Model"),
            FirmwareVersion.Create("1.0.0"),
            label: "initial");
        d.Activate();
        return d;
    }
}
