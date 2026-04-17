using Granit.IoT.EntityFrameworkCore.Timescale.Internal;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Timescale.Tests.Internal;

public class TimescaleTelemetryEfCoreReaderRoutingTests
{
    [Fact]
    public void Windows_of_one_day_or_more_use_the_daily_aggregate()
    {
        TimescaleTelemetryEfCoreReader
            .SelectAggregateView(TimeSpan.FromDays(1))
            .ShouldBe(TimescaleSqlBuilder.DailyAggregateView);

        TimescaleTelemetryEfCoreReader
            .SelectAggregateView(TimeSpan.FromDays(7))
            .ShouldBe(TimescaleSqlBuilder.DailyAggregateView);
    }

    [Fact]
    public void Windows_between_one_hour_and_one_day_use_the_hourly_aggregate()
    {
        TimescaleTelemetryEfCoreReader
            .SelectAggregateView(TimeSpan.FromHours(1))
            .ShouldBe(TimescaleSqlBuilder.HourlyAggregateView);

        TimescaleTelemetryEfCoreReader
            .SelectAggregateView(TimeSpan.FromHours(23))
            .ShouldBe(TimescaleSqlBuilder.HourlyAggregateView);
    }

    [Fact]
    public void Sub_hourly_windows_fall_back_to_the_raw_hypertable()
    {
        TimescaleTelemetryEfCoreReader
            .SelectAggregateView(TimeSpan.FromMinutes(59))
            .ShouldBeNull();

        TimescaleTelemetryEfCoreReader
            .SelectAggregateView(TimeSpan.FromMinutes(5))
            .ShouldBeNull();
    }
}
