using Granit.IoT.EntityFrameworkCore.Postgres.Internal;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Tests.Internal;

public sealed class TelemetryPartitionSqlBuilderTests
{
    [Theory]
    [InlineData(2026, 4, "iot_telemetry_points_2026_04")]
    [InlineData(2026, 12, "iot_telemetry_points_2026_12")]
    [InlineData(2027, 1, "iot_telemetry_points_2027_01")]
    public void PartitionName_FormatsYearMonth(int year, int month, string expected) =>
        TelemetryPartitionSqlBuilder.PartitionName(year, month).ShouldBe(expected);

    [Fact]
    public void EnablePartitioningSql_IsIdempotent_AndChecksPgPartitionedTable()
    {
        string sql = TelemetryPartitionSqlBuilder.EnablePartitioningSql();

        sql.ShouldContain("pg_partitioned_table");
        sql.ShouldContain("DO $$");
        sql.ShouldContain("PARTITION BY RANGE");
        sql.ShouldContain("\"RecordedAt\"");
    }

    [Fact]
    public void CreatePartitionSql_ContainsRangeBoundsAndIndexes()
    {
        string sql = TelemetryPartitionSqlBuilder.CreatePartitionSql(2026, 4);

        sql.ShouldContain("CREATE TABLE IF NOT EXISTS \"iot_telemetry_points_2026_04\"");
        sql.ShouldContain("PARTITION OF \"iot_telemetry_points\"");
        sql.ShouldContain("FOR VALUES FROM ('2026-04-01') TO ('2026-05-01')");
        sql.ShouldContain("CREATE INDEX IF NOT EXISTS ix_iot_telemetry_points_2026_04_brin_recorded_at");
        sql.ShouldContain("USING brin (\"RecordedAt\")");
        sql.ShouldContain("CREATE INDEX IF NOT EXISTS ix_iot_telemetry_points_2026_04_gin_metrics");
        sql.ShouldContain("USING gin (\"Metrics\")");
    }

    [Fact]
    public void CreatePartitionSql_DecemberRollsToNextYear()
    {
        string sql = TelemetryPartitionSqlBuilder.CreatePartitionSql(2026, 12);

        sql.ShouldContain("FOR VALUES FROM ('2026-12-01') TO ('2027-01-01')");
    }

    [Fact]
    public void CreatePartitionSql_QualifiesWithSchemaWhenProvided()
    {
        string sql = TelemetryPartitionSqlBuilder.CreatePartitionSql(2026, 4, schema: "iot");

        sql.ShouldContain("\"iot\".\"iot_telemetry_points_2026_04\"");
        sql.ShouldContain("\"iot\".\"iot_telemetry_points\"");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void CreatePartitionSql_RejectsInvalidMonth(int month) =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            TelemetryPartitionSqlBuilder.CreatePartitionSql(2026, month));

    [Theory]
    [InlineData(1899)]
    [InlineData(10000)]
    public void CreatePartitionSql_RejectsInvalidYear(int year) =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            TelemetryPartitionSqlBuilder.CreatePartitionSql(year, 6));
}
