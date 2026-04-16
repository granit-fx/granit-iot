using Granit.IoT.Domain;
using Shouldly;

namespace Granit.IoT.Tests.Domain;

public sealed class TelemetryPointTests
{
    [Fact]
    public void Create_ValidMetrics_Succeeds()
    {
        var metrics = new Dictionary<string, double>
        {
            ["temperature"] = 22.5,
            ["humidity"] = 45.0,
        };

        var point = TelemetryPoint.Create(
            Guid.NewGuid(),
            deviceId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            recordedAt: DateTimeOffset.UtcNow,
            metrics: metrics,
            messageId: "msg-001",
            source: "scaleway");

        point.Metrics.Count.ShouldBe(2);
        point.Metrics["temperature"].ShouldBe(22.5);
        point.MessageId.ShouldBe("msg-001");
        point.Source.ShouldBe("scaleway");
    }

    [Fact]
    public void Create_EmptyMetrics_Throws()
    {
        var metrics = new Dictionary<string, double>();

        Should.Throw<ArgumentException>(() => TelemetryPoint.Create(
            Guid.NewGuid(),
            deviceId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            recordedAt: DateTimeOffset.UtcNow,
            metrics: metrics));
    }

    [Fact]
    public void Create_NullMetrics_Throws()
    {
        Should.Throw<ArgumentNullException>(() => TelemetryPoint.Create(
            Guid.NewGuid(),
            deviceId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            recordedAt: DateTimeOffset.UtcNow,
            metrics: null!));
    }

    [Fact]
    public void Create_DefensiveCopy_PreventsMutation()
    {
        var original = new Dictionary<string, double> { ["temp"] = 20.0 };

        var point = TelemetryPoint.Create(
            Guid.NewGuid(),
            deviceId: Guid.NewGuid(),
            tenantId: null,
            recordedAt: DateTimeOffset.UtcNow,
            metrics: original);

        original["temp"] = 99.0;

        point.Metrics["temp"].ShouldBe(20.0);
    }
}
