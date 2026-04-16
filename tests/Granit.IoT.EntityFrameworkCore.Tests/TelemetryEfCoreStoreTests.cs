using Granit.IoT.Domain;
using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.MultiTenancy;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Tests;

public sealed class TelemetryEfCoreStoreTests : IDisposable
{
    private readonly TestDbContextFactory _factory = TestDbContextFactory.Create();
    private readonly TelemetryEfCoreReader _reader;
    private readonly TelemetryEfCoreWriter _writer;
    private readonly DeviceEfCoreWriter _deviceWriter;
    private readonly Guid _deviceId = Guid.NewGuid();

    public TelemetryEfCoreStoreTests()
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        _reader = new TelemetryEfCoreReader(_factory, currentTenant);
        _writer = new TelemetryEfCoreWriter(_factory, currentTenant);
        _deviceWriter = new DeviceEfCoreWriter(_factory, currentTenant);

        // Seed a device for FK integrity
        var device = Device.Create(
            _deviceId,
            tenantId: null,
            DeviceSerialNumber.Create("TELEMETRY-DEVICE"),
            HardwareModel.Create("Sensor-V1"),
            FirmwareVersion.Create("1.0.0"));
        _deviceWriter.AddAsync(device).GetAwaiter().GetResult();
    }

    public void Dispose() => _factory.Dispose();

    // ===== APPEND & QUERY =====

    [Fact]
    public async Task AppendAsync_ThenQueryAsync_ReturnsPoint()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        TelemetryPoint point = CreatePoint(now);

        await _writer.AppendAsync(point, TestContext.Current.CancellationToken);

        IReadOnlyList<TelemetryPoint> results = await _reader.QueryAsync(
            _deviceId,
            now.AddMinutes(-1),
            now.AddMinutes(1),
            cancellationToken: TestContext.Current.CancellationToken);

        results.Count.ShouldBeGreaterThanOrEqualTo(1);
        results.ShouldContain(p => p.Id == point.Id);
    }

    // ===== BATCH INSERT =====

    [Fact]
    public async Task AppendBatchAsync_InsertsMultiplePoints()
    {
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        var points = Enumerable.Range(0, 10)
            .Select(i => CreatePoint(baseTime.AddSeconds(i), $"batch-msg-{i}"))
            .ToList();

        await _writer.AppendBatchAsync(points, TestContext.Current.CancellationToken);

        IReadOnlyList<TelemetryPoint> results = await _reader.QueryAsync(
            _deviceId,
            baseTime.AddSeconds(-1),
            baseTime.AddSeconds(11),
            maxPoints: 100,
            cancellationToken: TestContext.Current.CancellationToken);

        results.Count.ShouldBe(10);
    }

    [Fact]
    public async Task AppendBatchAsync_EmptyList_DoesNothing()
    {
        await _writer.AppendBatchAsync([], TestContext.Current.CancellationToken);
    }

    // ===== GET LATEST =====

    [Fact]
    public async Task GetLatestAsync_ReturnsMostRecentPoint()
    {
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        await _writer.AppendAsync(CreatePoint(baseTime.AddMinutes(-10), "old"), TestContext.Current.CancellationToken);
        await _writer.AppendAsync(CreatePoint(baseTime, "latest"), TestContext.Current.CancellationToken);
        await _writer.AppendAsync(CreatePoint(baseTime.AddMinutes(-5), "middle"), TestContext.Current.CancellationToken);

        TelemetryPoint? result = await _reader.GetLatestAsync(_deviceId, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.MessageId.ShouldBe("latest");
    }

    [Fact]
    public async Task GetLatestAsync_NoData_ReturnsNull()
    {
        TelemetryPoint? result = await _reader.GetLatestAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    // ===== QUERY ORDERING =====

    [Fact]
    public async Task QueryAsync_ReturnsDescendingByRecordedAt()
    {
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        await _writer.AppendAsync(CreatePoint(baseTime.AddMinutes(-2), "first"), TestContext.Current.CancellationToken);
        await _writer.AppendAsync(CreatePoint(baseTime, "third"), TestContext.Current.CancellationToken);
        await _writer.AppendAsync(CreatePoint(baseTime.AddMinutes(-1), "second"), TestContext.Current.CancellationToken);

        IReadOnlyList<TelemetryPoint> results = await _reader.QueryAsync(
            _deviceId,
            baseTime.AddMinutes(-3),
            baseTime.AddMinutes(1),
            cancellationToken: TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
        results[0].MessageId.ShouldBe("third");
        results[1].MessageId.ShouldBe("second");
        results[2].MessageId.ShouldBe("first");
    }

    // ===== QUERY MAX POINTS =====

    [Fact]
    public async Task QueryAsync_RespectsMaxPoints()
    {
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            await _writer.AppendAsync(
                CreatePoint(baseTime.AddSeconds(i)),
                TestContext.Current.CancellationToken);
        }

        IReadOnlyList<TelemetryPoint> results = await _reader.QueryAsync(
            _deviceId,
            baseTime.AddSeconds(-1),
            baseTime.AddSeconds(6),
            maxPoints: 3,
            cancellationToken: TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
    }

    // ===== METRICS SERIALIZATION =====

    [Fact]
    public async Task AppendAsync_MetricsRoundTrip_PreservesValues()
    {
        var metrics = new Dictionary<string, double>
        {
            ["temperature"] = 22.5,
            ["humidity"] = 45.0,
            ["battery"] = 90.0,
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var point = TelemetryPoint.Create(
            Guid.NewGuid(), _deviceId, null, now, metrics, "roundtrip");

        await _writer.AppendAsync(point, TestContext.Current.CancellationToken);

        TelemetryPoint? result = await _reader.GetLatestAsync(_deviceId, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Metrics.Count.ShouldBe(3);
        result.Metrics["temperature"].ShouldBe(22.5);
        result.Metrics["humidity"].ShouldBe(45.0);
        result.Metrics["battery"].ShouldBe(90.0);
    }

    // ===== HELPERS =====

    private TelemetryPoint CreatePoint(
        DateTimeOffset recordedAt,
        string? messageId = null) =>
        TelemetryPoint.Create(
            Guid.NewGuid(),
            _deviceId,
            tenantId: null,
            recordedAt,
            new Dictionary<string, double> { ["temperature"] = 22.5, ["humidity"] = 45.0 },
            messageId);
}
