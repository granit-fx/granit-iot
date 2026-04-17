using Granit.IoT.EntityFrameworkCore.Postgres.Internal;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Extensions;

/// <summary>
/// Provides raw SQL migration steps for PostgreSQL-specific indexes
/// that EF Core cannot generate declaratively.
/// </summary>
/// <remarks>
/// Call these in your migration's <c>Up()</c> method after the
/// standard EF Core table creation.
/// </remarks>
public static class IoTPostgresMigrationExtensions
{
    /// <summary>
    /// Creates a BRIN index on <c>iot_telemetry_points.recorded_at</c> for
    /// efficient time-range scans on append-only data.
    /// </summary>
    public static MigrationBuilder CreateTelemetryBrinIndex(
        this MigrationBuilder migrationBuilder,
        string? schema = null)
    {
        string table = schema is not null
            ? $"\"{schema}\".\"iot_telemetry_points\""
            : "\"iot_telemetry_points\"";

        migrationBuilder.Sql(
            $"CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_iot_telemetry_brin_recorded_at ON {table} USING brin (\"RecordedAt\");");
        return migrationBuilder;
    }

    /// <summary>
    /// Creates a GIN index on <c>iot_telemetry_points.Metrics</c> for
    /// efficient jsonb containment queries.
    /// </summary>
    public static MigrationBuilder CreateTelemetryGinIndex(
        this MigrationBuilder migrationBuilder,
        string? schema = null)
    {
        string table = schema is not null
            ? $"\"{schema}\".\"iot_telemetry_points\""
            : "\"iot_telemetry_points\"";

        migrationBuilder.Sql(
            $"CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_iot_telemetry_gin_metrics ON {table} USING gin (\"Metrics\");");
        return migrationBuilder;
    }

    /// <summary>
    /// Creates all PostgreSQL-specific indexes for the IoT module in one call.
    /// </summary>
    public static MigrationBuilder CreateIoTPostgresIndexes(
        this MigrationBuilder migrationBuilder,
        string? schema = null)
    {
        migrationBuilder.CreateTelemetryBrinIndex(schema);
        migrationBuilder.CreateTelemetryGinIndex(schema);
        return migrationBuilder;
    }

    /// <summary>
    /// Converts <c>iot_telemetry_points</c> to a RANGE-partitioned table by
    /// <c>RecordedAt</c>. Idempotent: the DDL inspects <c>pg_partitioned_table</c>
    /// and skips if already partitioned. Designed for empty tables — converting
    /// a populated table requires a separate data-copy migration which is out of
    /// scope here.
    /// </summary>
    /// <remarks>
    /// Pair this call with one or more <see cref="CreateTelemetryPartition"/>
    /// invocations to seed the first months. Future months are created at
    /// runtime by <c>TelemetryPartitionMaintenanceJob</c>.
    /// </remarks>
    public static MigrationBuilder EnableTelemetryPartitioning(
        this MigrationBuilder migrationBuilder,
        string? schema = null)
    {
        migrationBuilder.Sql(TelemetryPartitionSqlBuilder.EnablePartitioningSql(schema));
        return migrationBuilder;
    }

    /// <summary>
    /// Creates a single monthly partition <c>iot_telemetry_points_{year}_{month:D2}</c>
    /// covering <c>[firstOfMonth, firstOfNextMonth)</c>, attached to the parent.
    /// Uses <c>CREATE TABLE IF NOT EXISTS</c> + <c>IF NOT EXISTS</c> on the
    /// partition-local BRIN(RecordedAt) and GIN(Metrics) indexes for idempotency.
    /// </summary>
    public static MigrationBuilder CreateTelemetryPartition(
        this MigrationBuilder migrationBuilder,
        int year,
        int month,
        string? schema = null)
    {
        migrationBuilder.Sql(TelemetryPartitionSqlBuilder.CreatePartitionSql(year, month, schema));
        return migrationBuilder;
    }
}
