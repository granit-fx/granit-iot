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
}
