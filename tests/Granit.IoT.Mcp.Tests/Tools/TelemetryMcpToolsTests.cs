using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.Mcp.Responses;
using Granit.IoT.Mcp.Tools;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Mcp.Tests.Tools;

public class TelemetryMcpToolsTests
{
    private static readonly DateTimeOffset From = new(2026, 4, 17, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddHours(1);

    [Fact]
    public async Task QueryAsync_CapsMaxPointsAt1000()
    {
        ITelemetryReader reader = Substitute.For<ITelemetryReader>();
        var deviceId = Guid.NewGuid();
        reader.QueryAsync(deviceId, From, To, 1000, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TelemetryPoint>());

        await TelemetryMcpTools.QueryAsync(
            reader, deviceId, "temperature", From, To, maxPoints: 5000,
            cancellationToken: TestContext.Current.CancellationToken);

        await reader.Received(1).QueryAsync(deviceId, From, To, 1000, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryAsync_ClampsMinimumMaxPointsToOne()
    {
        ITelemetryReader reader = Substitute.For<ITelemetryReader>();
        var deviceId = Guid.NewGuid();
        reader.QueryAsync(deviceId, From, To, 1, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TelemetryPoint>());

        await TelemetryMcpTools.QueryAsync(
            reader, deviceId, "temperature", From, To, maxPoints: -5,
            cancellationToken: TestContext.Current.CancellationToken);

        await reader.Received(1).QueryAsync(deviceId, From, To, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryAsync_FiltersByMetricName()
    {
        ITelemetryReader reader = Substitute.For<ITelemetryReader>();
        var deviceId = Guid.NewGuid();

        TelemetryPoint withTemp = BuildPoint(deviceId, From.AddMinutes(10), new()
        {
            ["temperature"] = 4.1,
            ["humidity"] = 60,
        });
        TelemetryPoint humidityOnly = BuildPoint(deviceId, From.AddMinutes(20), new()
        {
            ["humidity"] = 62,
        });

        reader.QueryAsync(deviceId, From, To, 100, Arg.Any<CancellationToken>())
            .Returns(new[] { withTemp, humidityOnly });

        IReadOnlyList<TelemetryReadingMcpResponse> result = await TelemetryMcpTools.QueryAsync(
            reader, deviceId, "temperature", From, To,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].MetricName.ShouldBe("temperature");
        result[0].Value.ShouldBe(4.1);
    }

    [Fact]
    public async Task QueryAsync_NullReader_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            TelemetryMcpTools.QueryAsync(
                null!, Guid.NewGuid(), "temperature", From, To,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task QueryAsync_EmptyMetricName_Throws()
    {
        ITelemetryReader reader = Substitute.For<ITelemetryReader>();
        await Should.ThrowAsync<ArgumentException>(() =>
            TelemetryMcpTools.QueryAsync(
                reader, Guid.NewGuid(), "   ", From, To,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLatestReadingsAsync_NoTelemetry_ReturnsEmpty()
    {
        ITelemetryReader reader = Substitute.For<ITelemetryReader>();
        var deviceId = Guid.NewGuid();
        reader.GetLatestAsync(deviceId, Arg.Any<CancellationToken>()).Returns((TelemetryPoint?)null);

        IReadOnlyList<TelemetryReadingMcpResponse> result = await TelemetryMcpTools.GetLatestReadingsAsync(
            reader, deviceId, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLatestReadingsAsync_ExpandsMetricsDictionary()
    {
        ITelemetryReader reader = Substitute.For<ITelemetryReader>();
        var deviceId = Guid.NewGuid();
        DateTimeOffset recordedAt = new(2026, 4, 17, 14, 30, 0, TimeSpan.Zero);
        TelemetryPoint point = BuildPoint(deviceId, recordedAt, new()
        {
            ["temperature"] = 3.8,
            ["humidity"] = 55,
            ["voltage"] = 3.3,
        });
        reader.GetLatestAsync(deviceId, Arg.Any<CancellationToken>()).Returns(point);

        IReadOnlyList<TelemetryReadingMcpResponse> result = await TelemetryMcpTools.GetLatestReadingsAsync(
            reader, deviceId, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(3);
        result.ShouldAllBe(r => r.RecordedAt == recordedAt);
        result.Select(r => r.MetricName).ShouldBe(
            new[] { "temperature", "humidity", "voltage" }, ignoreOrder: true);
    }

    private static TelemetryPoint BuildPoint(
        Guid deviceId,
        DateTimeOffset recordedAt,
        Dictionary<string, double> metrics) =>
        TelemetryPoint.Create(
            id: Guid.NewGuid(),
            deviceId: deviceId,
            tenantId: Guid.NewGuid(),
            recordedAt: recordedAt,
            metrics: metrics);
}
