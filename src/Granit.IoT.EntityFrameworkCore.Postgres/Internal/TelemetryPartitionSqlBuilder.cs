using System.Globalization;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Internal;

/// <summary>
/// Single source of truth for the DDL that creates monthly partitions on
/// <c>iot_telemetry_points</c>. Used by both the <c>MigrationBuilder</c>
/// helpers (called by consumer apps in their EF migrations) and the runtime
/// <c>TelemetryPartitionMaintenanceJob</c>. Centralising the SQL keeps
/// partition naming and index attachment identical across both call sites.
/// </summary>
internal static class TelemetryPartitionSqlBuilder
{
    private const string ParentTable = "iot_telemetry_points";

    public static string PartitionName(int year, int month) =>
        $"{ParentTable}_{year:D4}_{month:D2}";

    /// <summary>
    /// Returns DDL that converts the parent table to RANGE-partitioned by
    /// <c>RecordedAt</c>. Idempotent: a DO-block inspects
    /// <c>pg_partitioned_table</c> first and skips if already partitioned.
    /// Designed for empty tables — converting a populated table requires a
    /// separate data-copy migration.
    /// </summary>
    public static string EnablePartitioningSql(string? schema = null)
    {
        string qualified = QualifiedName(ParentTable, schema);
        return $@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_partitioned_table pt
        JOIN pg_class c ON c.oid = pt.partrelid
        WHERE c.relname = '{ParentTable}'
    ) THEN
        EXECUTE 'ALTER TABLE {qualified} RENAME TO {ParentTable}_legacy';
        EXECUTE 'CREATE TABLE {qualified} (LIKE {QualifiedName(ParentTable + "_legacy", schema)} INCLUDING ALL) PARTITION BY RANGE (""RecordedAt"")';
        EXECUTE 'DROP TABLE {QualifiedName(ParentTable + "_legacy", schema)}';
    END IF;
END $$;";
    }

    /// <summary>
    /// Returns DDL that creates the partition for the given (year, month) attached
    /// to the parent table, plus partition-local BRIN(RecordedAt) and GIN(Metrics)
    /// indexes. All operations use <c>IF NOT EXISTS</c> for idempotency.
    /// </summary>
    public static string CreatePartitionSql(int year, int month, string? schema = null)
    {
        if (year is < 1900 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        string partition = PartitionName(year, month);
        string qualifiedPartition = QualifiedName(partition, schema);
        string qualifiedParent = QualifiedName(ParentTable, schema);

        DateTime from = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime to = from.AddMonths(1);
        string fromLiteral = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string toLiteral = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return $@"
CREATE TABLE IF NOT EXISTS {qualifiedPartition}
    PARTITION OF {qualifiedParent}
    FOR VALUES FROM ('{fromLiteral}') TO ('{toLiteral}');
CREATE INDEX IF NOT EXISTS ix_{partition}_brin_recorded_at
    ON {qualifiedPartition} USING brin (""RecordedAt"");
CREATE INDEX IF NOT EXISTS ix_{partition}_gin_metrics
    ON {qualifiedPartition} USING gin (""Metrics"");";
    }

    private static string QualifiedName(string table, string? schema) =>
        schema is null ? $"\"{table}\"" : $"\"{schema}\".\"{table}\"";
}
