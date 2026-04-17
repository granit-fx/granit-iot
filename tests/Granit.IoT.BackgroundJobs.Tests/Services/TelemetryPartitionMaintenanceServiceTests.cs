using Granit.IoT.Abstractions;
using Granit.IoT.BackgroundJobs.Services;
using Granit.IoT.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Granit.IoT.BackgroundJobs.Tests.Services;

public sealed class TelemetryPartitionMaintenanceServiceTests
{
    [Fact]
    public async Task ExecuteAsync_NotPartitioned_NoOp()
    {
        ITelemetryPartitionMaintainer maintainer = Substitute.For<ITelemetryPartitionMaintainer>();
        maintainer.IsParentPartitionedAsync(Arg.Any<CancellationToken>()).Returns(false);

        TelemetryPartitionMaintenanceService service = CreateService(maintainer, new(2026, 4, 17, 1, 0, 0, TimeSpan.Zero));

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        await maintainer.DidNotReceive().CreatePartitionAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Partitioned_CreatesNextTwoMonths()
    {
        // Now = 2026-04-17 → expects partitions for 2026-05 and 2026-06.
        ITelemetryPartitionMaintainer maintainer = Substitute.For<ITelemetryPartitionMaintainer>();
        maintainer.IsParentPartitionedAsync(Arg.Any<CancellationToken>()).Returns(true);

        TelemetryPartitionMaintenanceService service = CreateService(maintainer, new(2026, 4, 17, 1, 0, 0, TimeSpan.Zero));

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        await maintainer.Received(1).CreatePartitionAsync(2026, 5, Arg.Any<CancellationToken>());
        await maintainer.Received(1).CreatePartitionAsync(2026, 6, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Partitioned_DecemberRollsToNextYear()
    {
        // Now = 2026-12-15 → expects partitions for 2027-01 and 2027-02.
        ITelemetryPartitionMaintainer maintainer = Substitute.For<ITelemetryPartitionMaintainer>();
        maintainer.IsParentPartitionedAsync(Arg.Any<CancellationToken>()).Returns(true);

        TelemetryPartitionMaintenanceService service = CreateService(maintainer, new(2026, 12, 15, 1, 0, 0, TimeSpan.Zero));

        await service.ExecuteAsync(TestContext.Current.CancellationToken);

        await maintainer.Received(1).CreatePartitionAsync(2027, 1, Arg.Any<CancellationToken>());
        await maintainer.Received(1).CreatePartitionAsync(2027, 2, Arg.Any<CancellationToken>());
    }

    private static TelemetryPartitionMaintenanceService CreateService(
        ITelemetryPartitionMaintainer maintainer,
        DateTimeOffset now) =>
        new(
            maintainer,
            new IoTMetrics(new TestMeterFactory()),
            new FakeTimeProvider(now),
            NullLogger<TelemetryPartitionMaintenanceService>.Instance);
}
