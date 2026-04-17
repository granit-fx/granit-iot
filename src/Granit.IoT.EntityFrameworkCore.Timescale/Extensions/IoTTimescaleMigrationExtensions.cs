using Granit.IoT.EntityFrameworkCore.Timescale.Internal;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Granit.IoT.EntityFrameworkCore.Timescale.Extensions;

/// <summary>
/// Migration-based entry points for TimescaleDB. Teams who prefer to drive the
/// hypertable conversion and continuous aggregates from their deployment
/// pipeline (rather than at application startup via
/// <c>GranitIoTTimescaleModule.OnApplicationInitializationAsync</c>) can call
/// these helpers inside a standard EF Core migration.
/// </summary>
public static class IoTTimescaleMigrationExtensions
{
    /// <summary>
    /// Converts the telemetry table to a TimescaleDB hypertable with 7-day
    /// chunks. Idempotent. Safe to run on an empty table; uses
    /// <c>migrate_data =&gt; TRUE</c> to migrate existing rows.
    /// </summary>
    public static MigrationBuilder EnableTelemetryHypertable(this MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.Sql(TimescaleSqlBuilder.CreateHypertableSql());
        return migrationBuilder;
    }

    /// <summary>
    /// Creates the hourly continuous aggregate and attaches its refresh policy
    /// (start 3h ago, end 1h ago, every 30 minutes). Idempotent.
    /// </summary>
    public static MigrationBuilder CreateTelemetryHourlyAggregate(this MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.Sql(TimescaleSqlBuilder.CreateHourlyAggregateSql());
        migrationBuilder.Sql(TimescaleSqlBuilder.AddRefreshPolicySql(
            TimescaleSqlBuilder.HourlyAggregateView,
            startOffset: "3 hours",
            endOffset: "1 hour",
            scheduleInterval: "30 minutes"));
        return migrationBuilder;
    }

    /// <summary>
    /// Creates the daily continuous aggregate and attaches its refresh policy
    /// (start 3d ago, end 1d ago, every 6 hours). Idempotent.
    /// </summary>
    public static MigrationBuilder CreateTelemetryDailyAggregate(this MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.Sql(TimescaleSqlBuilder.CreateDailyAggregateSql());
        migrationBuilder.Sql(TimescaleSqlBuilder.AddRefreshPolicySql(
            TimescaleSqlBuilder.DailyAggregateView,
            startOffset: "3 days",
            endOffset: "1 day",
            scheduleInterval: "6 hours"));
        return migrationBuilder;
    }
}
