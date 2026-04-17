namespace Granit.IoT.EntityFrameworkCore.Timescale.Internal;

/// <summary>
/// Builds idempotent TimescaleDB DDL statements for the IoT telemetry table:
/// extension detection, hypertable conversion, and the hourly / daily
/// continuous aggregates plus their refresh policies. All SQL is parameter-free
/// so it can be executed with <c>ExecuteSqlRawAsync</c>.
/// </summary>
internal static class TimescaleSqlBuilder
{
    /// <summary>Materialized view name for the hourly continuous aggregate.</summary>
    internal const string HourlyAggregateView = "iot_telemetry_hourly";

    /// <summary>Materialized view name for the daily continuous aggregate.</summary>
    internal const string DailyAggregateView = "iot_telemetry_daily";

    /// <summary>Query to detect whether the timescaledb extension is installed.</summary>
    internal const string ExtensionCheckSql =
        "SELECT 1 FROM pg_extension WHERE extname = 'timescaledb'";

    /// <summary>Fully-qualified identifier of the telemetry table (schema-aware).</summary>
    internal static string QualifiedTelemetryTable()
    {
        string tableName = GranitIoTDbProperties.DbTablePrefix + "telemetry_points";
        string? schema = GranitIoTDbProperties.DbSchema;
        return string.IsNullOrEmpty(schema)
            ? $"\"{tableName}\""
            : $"\"{schema}\".\"{tableName}\"";
    }

    /// <summary>
    /// Converts the telemetry table to a hypertable partitioned on <c>RecordedAt</c>
    /// with 7-day chunks. Idempotent — <c>if_not_exists =&gt; TRUE</c> guards re-runs.
    /// </summary>
    internal static string CreateHypertableSql() =>
        $"SELECT create_hypertable({Literal(QualifiedTelemetryTable())}, 'RecordedAt', " +
        "chunk_time_interval => INTERVAL '7 days', if_not_exists => TRUE, migrate_data => TRUE)";

    /// <summary>
    /// Creates the hourly continuous aggregate. The view expands the JSONB
    /// <c>Metrics</c> column via <c>jsonb_each</c> so each metric/bucket pair
    /// becomes its own row with pre-computed avg/min/max/count. Idempotent.
    /// </summary>
    internal static string CreateHourlyAggregateSql() =>
        BuildContinuousAggregateSql(HourlyAggregateView, bucketInterval: "1 hour");

    /// <summary>Creates the daily continuous aggregate — same shape, 1-day buckets.</summary>
    internal static string CreateDailyAggregateSql() =>
        BuildContinuousAggregateSql(DailyAggregateView, bucketInterval: "1 day");

    /// <summary>
    /// Adds a refresh policy to <paramref name="viewName"/>. TimescaleDB rejects
    /// duplicate policies; callers guard with a <c>DO $$ BEGIN ... EXCEPTION
    /// WHEN duplicate_object ... END $$</c> block in <see cref="AddRefreshPolicySql"/>.
    /// </summary>
    /// <param name="viewName">Unqualified continuous-aggregate view name.</param>
    /// <param name="startOffset">How far back each refresh covers (PostgreSQL interval).</param>
    /// <param name="endOffset">How close to <c>now()</c> the refresh stops (interval).</param>
    /// <param name="scheduleInterval">How often the policy runs (interval).</param>
    internal static string AddRefreshPolicySql(
        string viewName,
        string startOffset,
        string endOffset,
        string scheduleInterval) =>
        $"""
        DO $$
        BEGIN
            PERFORM add_continuous_aggregate_policy({Literal(viewName)},
                start_offset => INTERVAL {Literal(startOffset)},
                end_offset => INTERVAL {Literal(endOffset)},
                schedule_interval => INTERVAL {Literal(scheduleInterval)});
        EXCEPTION
            WHEN duplicate_object THEN NULL;
        END $$;
        """;

    private static string BuildContinuousAggregateSql(string viewName, string bucketInterval)
    {
        // '{}' is the PostgreSQL empty JSON path — jsonb_value #>> '{}' returns the value as text.
        const string valueAsText = "(metric.value #>> '{}')::double precision";

        return
            $"CREATE MATERIALIZED VIEW IF NOT EXISTS \"{viewName}\" " +
            "WITH (timescaledb.continuous) AS " +
            "SELECT " +
            $"time_bucket(INTERVAL {Literal(bucketInterval)}, \"RecordedAt\") AS bucket, " +
            "\"DeviceId\", " +
            "\"TenantId\", " +
            "metric.key AS \"MetricName\", " +
            $"AVG({valueAsText}) AS avg_value, " +
            $"MIN({valueAsText}) AS min_value, " +
            $"MAX({valueAsText}) AS max_value, " +
            "COUNT(*) AS count " +
            $"FROM {QualifiedTelemetryTable()}, " +
            "LATERAL jsonb_each(\"Metrics\") AS metric " +
            "WHERE jsonb_typeof(metric.value) = 'number' " +
            "GROUP BY bucket, \"DeviceId\", \"TenantId\", metric.key " +
            "WITH NO DATA";
    }

    /// <summary>Single-quoted PostgreSQL string literal with inner quote doubling.</summary>
    private static string Literal(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
