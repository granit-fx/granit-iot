using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.Endpoints.Dtos;
using Granit.IoT.Endpoints.Endpoints;
using Granit.Timing;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Endpoints.Tests.Endpoints;

public sealed class TelemetryEndpointsTests
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task QueryTelemetryAsync_DeviceMissing_Returns404()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        IClock clock = StubClock();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns((Device?)null);

        Results<Ok<IReadOnlyList<TelemetryPointResponse>>, NotFound> result = await TelemetryEndpoints
            .QueryTelemetryAsync(DeviceId, devReader, telReader, clock, null, null, null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task QueryTelemetryAsync_DefaultsAndClampsMaxPoints()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        IClock clock = StubClock();
        Device d = NewDevice();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns(d);
        telReader.QueryAsync(DeviceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([NewPoint()]);

        Results<Ok<IReadOnlyList<TelemetryPointResponse>>, NotFound> result = await TelemetryEndpoints
            .QueryTelemetryAsync(DeviceId, devReader, telReader, clock, null, null, maxPoints: 999_999, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Ok<IReadOnlyList<TelemetryPointResponse>> ok = result.Result.ShouldBeOfType<Ok<IReadOnlyList<TelemetryPointResponse>>>();
        ok.Value.ShouldNotBeNull();
        ok.Value.Count.ShouldBe(1);
        await telReader.Received(1).QueryAsync(
            DeviceId,
            Now.AddHours(-24),
            Now,
            10000,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task QueryTelemetryAsync_ExplicitRange_Used()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        IClock clock = StubClock();
        Device d = NewDevice();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns(d);
        telReader.QueryAsync(DeviceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        DateTimeOffset start = Now.AddHours(-2);
        DateTimeOffset end = Now.AddHours(-1);

        await TelemetryEndpoints
            .QueryTelemetryAsync(DeviceId, devReader, telReader, clock, start, end, 250, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await telReader.Received(1).QueryAsync(DeviceId, start, end, 250, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task GetLatestTelemetryAsync_DeviceMissing_Returns404()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns((Device?)null);

        Results<Ok<TelemetryPointResponse>, NotFound> result = await TelemetryEndpoints
            .GetLatestTelemetryAsync(DeviceId, devReader, telReader, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetLatestTelemetryAsync_NoData_Returns404()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        Device d = NewDevice();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns(d);
        telReader.GetLatestAsync(DeviceId, Arg.Any<CancellationToken>()).Returns((TelemetryPoint?)null);

        Results<Ok<TelemetryPointResponse>, NotFound> result = await TelemetryEndpoints
            .GetLatestTelemetryAsync(DeviceId, devReader, telReader, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetLatestTelemetryAsync_HasPoint_ReturnsOk()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        Device d = NewDevice();
        TelemetryPoint pt = NewPoint();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns(d);
        telReader.GetLatestAsync(DeviceId, Arg.Any<CancellationToken>()).Returns(pt);

        Results<Ok<TelemetryPointResponse>, NotFound> result = await TelemetryEndpoints
            .GetLatestTelemetryAsync(DeviceId, devReader, telReader, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Ok<TelemetryPointResponse> ok = result.Result.ShouldBeOfType<Ok<TelemetryPointResponse>>();
        ok.Value!.Id.ShouldBe(pt.Id);
    }

    [Fact]
    public async Task GetTelemetryAggregateAsync_DeviceMissing_Returns404()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        IClock clock = StubClock();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns((Device?)null);

        Results<Ok<TelemetryAggregateResponse>, NotFound> result = await TelemetryEndpoints
            .GetTelemetryAggregateAsync(DeviceId, "temp", TelemetryAggregation.Avg,
                devReader, telReader, clock, null, null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetTelemetryAggregateAsync_NoAggregate_Returns404()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        IClock clock = StubClock();
        Device d = NewDevice();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns(d);
        telReader.GetAggregateAsync(DeviceId, "temp", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TelemetryAggregation.Avg, Arg.Any<CancellationToken>())
            .Returns((TelemetryAggregate?)null);

        Results<Ok<TelemetryAggregateResponse>, NotFound> result = await TelemetryEndpoints
            .GetTelemetryAggregateAsync(DeviceId, "temp", TelemetryAggregation.Avg,
                devReader, telReader, clock, null, null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetTelemetryAggregateAsync_AggregateExists_ReturnsResponse()
    {
        IDeviceReader devReader = Substitute.For<IDeviceReader>();
        ITelemetryReader telReader = Substitute.For<ITelemetryReader>();
        IClock clock = StubClock();
        Device d = NewDevice();
        devReader.FindAsync(DeviceId, Arg.Any<CancellationToken>()).Returns(d);
        DateTimeOffset start = Now.AddHours(-1);
        DateTimeOffset end = Now;
        TelemetryAggregate agg = new(42.0, 100, start, end);
        telReader.GetAggregateAsync(DeviceId, "temp", start, end, TelemetryAggregation.Max, Arg.Any<CancellationToken>())
            .Returns(agg);

        Results<Ok<TelemetryAggregateResponse>, NotFound> result = await TelemetryEndpoints
            .GetTelemetryAggregateAsync(DeviceId, "temp", TelemetryAggregation.Max,
                devReader, telReader, clock, start, end, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Ok<TelemetryAggregateResponse> ok = result.Result.ShouldBeOfType<Ok<TelemetryAggregateResponse>>();
        ok.Value!.Value.ShouldBe(42.0);
        ok.Value.Count.ShouldBe(100);
        ok.Value.Aggregation.ShouldBe("Max");
        ok.Value.MetricName.ShouldBe("temp");
    }

    private static IClock StubClock()
    {
        IClock c = Substitute.For<IClock>();
        c.Now.Returns(Now);
        return c;
    }

    private static Device NewDevice()
    {
        var d = Device.Create(
            DeviceId, tenantId: null,
            DeviceSerialNumber.Create("SN-1"),
            HardwareModel.Create("Model"),
            FirmwareVersion.Create("1.0.0"));
        d.Activate();
        return d;
    }

    private static TelemetryPoint NewPoint() => TelemetryPoint.Create(
        id: Guid.NewGuid(),
        deviceId: DeviceId,
        tenantId: null,
        recordedAt: Now,
        metrics: new Dictionary<string, double> { ["temp"] = 22.5 },
        messageId: "msg-1",
        source: "test");
}
