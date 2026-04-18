using Granit.IoT.EntityFrameworkCore.Timescale.Internal;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Timescale.Tests.Internal;

public sealed class TimescaleTelemetryEfCoreReaderSelectionTests
{
    [Fact]
    public void SelectAggregateView_Daily_WhenWindowAtLeastOneDay()
    {
        string? view = TimescaleTelemetryEfCoreReader.SelectAggregateView(TimeSpan.FromDays(1));

        view.ShouldBe(TimescaleSqlBuilder.DailyAggregateView);
    }

    [Fact]
    public void SelectAggregateView_Daily_WhenWindowExceedsADay()
    {
        string? view = TimescaleTelemetryEfCoreReader.SelectAggregateView(TimeSpan.FromDays(7));

        view.ShouldBe(TimescaleSqlBuilder.DailyAggregateView);
    }

    [Fact]
    public void SelectAggregateView_Hourly_WhenWindowAtLeastOneHourButLessThanADay()
    {
        string? view = TimescaleTelemetryEfCoreReader.SelectAggregateView(TimeSpan.FromHours(2));

        view.ShouldBe(TimescaleSqlBuilder.HourlyAggregateView);
    }

    [Fact]
    public void SelectAggregateView_Null_WhenWindowSubHour()
    {
        string? view = TimescaleTelemetryEfCoreReader.SelectAggregateView(TimeSpan.FromMinutes(30));

        view.ShouldBeNull();
    }
}
