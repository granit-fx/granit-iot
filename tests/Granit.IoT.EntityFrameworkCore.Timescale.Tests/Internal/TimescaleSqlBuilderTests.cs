using Granit.IoT.EntityFrameworkCore.Timescale.Internal;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Timescale.Tests.Internal;

public class TimescaleSqlBuilderTests
{
    [Fact]
    public void CreateHypertableSql_IsIdempotentAndMigratesData()
    {
        string sql = TimescaleSqlBuilder.CreateHypertableSql();

        sql.ShouldContain("create_hypertable");
        sql.ShouldContain("if_not_exists => TRUE", Case.Sensitive);
        sql.ShouldContain("migrate_data => TRUE", Case.Sensitive);
        sql.ShouldContain("'RecordedAt'");
        sql.ShouldContain("INTERVAL '7 days'");
    }

    [Fact]
    public void CreateHourlyAggregateSql_IsMarkedContinuousAndBucketsByHour()
    {
        string sql = TimescaleSqlBuilder.CreateHourlyAggregateSql();

        sql.ShouldContain($"CREATE MATERIALIZED VIEW IF NOT EXISTS \"{TimescaleSqlBuilder.HourlyAggregateView}\"");
        sql.ShouldContain("WITH (timescaledb.continuous)");
        sql.ShouldContain("time_bucket(INTERVAL '1 hour'");
        sql.ShouldContain("jsonb_each(\"Metrics\")");
        sql.ShouldContain("jsonb_typeof(metric.value) = 'number'");
        sql.ShouldContain("WITH NO DATA");
    }

    [Fact]
    public void CreateDailyAggregateSql_UsesDayBucket()
    {
        string sql = TimescaleSqlBuilder.CreateDailyAggregateSql();

        sql.ShouldContain($"CREATE MATERIALIZED VIEW IF NOT EXISTS \"{TimescaleSqlBuilder.DailyAggregateView}\"");
        sql.ShouldContain("time_bucket(INTERVAL '1 day'");
    }

    [Fact]
    public void AddRefreshPolicySql_IsGuardedAgainstDuplicatePolicy()
    {
        string sql = TimescaleSqlBuilder.AddRefreshPolicySql(
            TimescaleSqlBuilder.HourlyAggregateView,
            startOffset: "3 hours",
            endOffset: "1 hour",
            scheduleInterval: "30 minutes");

        sql.ShouldContain("add_continuous_aggregate_policy");
        sql.ShouldContain($"'{TimescaleSqlBuilder.HourlyAggregateView}'");
        sql.ShouldContain("INTERVAL '3 hours'");
        sql.ShouldContain("INTERVAL '1 hour'");
        sql.ShouldContain("INTERVAL '30 minutes'");
        sql.ShouldContain("WHEN duplicate_object THEN NULL");
    }

    [Fact]
    public void Literal_EscapesSingleQuotes()
    {
        // The only way to exercise the escape is via a caller that can accept a quote-bearing value.
        // AddRefreshPolicySql funnels the viewName through Literal.
        string sql = TimescaleSqlBuilder.AddRefreshPolicySql(
            "O'Malley",
            startOffset: "1 hour",
            endOffset: "0",
            scheduleInterval: "10 minutes");

        sql.ShouldContain("'O''Malley'", Case.Sensitive);
    }
}
