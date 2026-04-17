using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.IoT.EntityFrameworkCore.Timescale.Extensions;
using Granit.IoT.EntityFrameworkCore.Timescale.Internal;
using Granit.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.EntityFrameworkCore.Timescale;

/// <summary>
/// Opt-in TimescaleDB backend for Granit.IoT. Converts the telemetry table to
/// a hypertable, installs hourly and daily continuous aggregates, and replaces
/// the telemetry reader. All DDL runs during
/// <see cref="OnApplicationInitializationAsync"/> using idempotent
/// <c>IF NOT EXISTS</c> / <c>if_not_exists =&gt; TRUE</c> guards so the module
/// can start safely against an existing TimescaleDB deployment or a cluster
/// where the extension is not yet installed.
/// </summary>
[DependsOn(typeof(GranitIoTEntityFrameworkCoreModule))]
public sealed class GranitIoTTimescaleModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTTimescale();
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ILogger<GranitIoTTimescaleModule> logger = context.ServiceProvider
            .GetRequiredService<ILogger<GranitIoTTimescaleModule>>();

        IDbContextFactory<IoTDbContext>? factory = context.ServiceProvider
            .GetService<IDbContextFactory<IoTDbContext>>();

        if (factory is null)
        {
            logger.LogWarning(
                "GranitIoTTimescaleModule is loaded but no IDbContextFactory<IoTDbContext> is registered. " +
                "Hypertable conversion skipped. Register Granit.IoT.EntityFrameworkCore before this module.");
            return;
        }

        await using IoTDbContext db = await factory.CreateDbContextAsync().ConfigureAwait(false);

        if (!await IsTimescaleExtensionInstalledAsync(db).ConfigureAwait(false))
        {
            logger.LogWarning(
                "timescaledb extension is not installed on the IoT database. Hypertable conversion and " +
                "continuous aggregates skipped. Install the extension (CREATE EXTENSION timescaledb;) and " +
                "restart the application to enable TimescaleDB features.");
            return;
        }

        await EnsureHypertableAsync(db, logger).ConfigureAwait(false);
        await EnsureContinuousAggregatesAsync(db, logger).ConfigureAwait(false);
    }

    private static async Task<bool> IsTimescaleExtensionInstalledAsync(IoTDbContext db)
    {
        List<int> rows = await db.Database
            .SqlQueryRaw<int>(TimescaleSqlBuilder.ExtensionCheckSql)
            .ToListAsync().ConfigureAwait(false);
        return rows.Count > 0;
    }

    private static async Task EnsureHypertableAsync(IoTDbContext db, ILogger logger)
    {
        await db.Database
            .ExecuteSqlRawAsync(TimescaleSqlBuilder.CreateHypertableSql())
            .ConfigureAwait(false);
        logger.LogInformation("Granit.IoT telemetry table is now a TimescaleDB hypertable (7-day chunks).");
    }

    private static async Task EnsureContinuousAggregatesAsync(IoTDbContext db, ILogger logger)
    {
        await db.Database
            .ExecuteSqlRawAsync(TimescaleSqlBuilder.CreateHourlyAggregateSql())
            .ConfigureAwait(false);
        await db.Database
            .ExecuteSqlRawAsync(TimescaleSqlBuilder.AddRefreshPolicySql(
                TimescaleSqlBuilder.HourlyAggregateView,
                startOffset: "3 hours",
                endOffset: "1 hour",
                scheduleInterval: "30 minutes"))
            .ConfigureAwait(false);

        await db.Database
            .ExecuteSqlRawAsync(TimescaleSqlBuilder.CreateDailyAggregateSql())
            .ConfigureAwait(false);
        await db.Database
            .ExecuteSqlRawAsync(TimescaleSqlBuilder.AddRefreshPolicySql(
                TimescaleSqlBuilder.DailyAggregateView,
                startOffset: "3 days",
                endOffset: "1 day",
                scheduleInterval: "6 hours"))
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Granit.IoT continuous aggregates ready: {Hourly} (refresh every 30m), {Daily} (refresh every 6h).",
                TimescaleSqlBuilder.HourlyAggregateView,
                TimescaleSqlBuilder.DailyAggregateView);
        }
    }
}
