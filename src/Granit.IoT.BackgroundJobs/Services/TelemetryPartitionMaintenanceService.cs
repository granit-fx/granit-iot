using Granit.IoT.Abstractions;
using Granit.IoT.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.BackgroundJobs.Services;

/// <summary>
/// Creates the next two monthly partitions for <c>iot_telemetry_points</c>
/// so ingestion never fails on a month boundary. Idempotent (<c>CREATE TABLE
/// IF NOT EXISTS</c>). Gracefully no-ops when the parent table is not
/// partitioned — production opts into partitioning via
/// <c>EnableTelemetryPartitioning()</c> in a migration; without it the job
/// logs a warning and exits.
/// </summary>
public sealed partial class TelemetryPartitionMaintenanceService(
    ITelemetryPartitionMaintainer maintainer,
    IoTMetrics metrics,
    TimeProvider clock,
    ILogger<TelemetryPartitionMaintenanceService> logger)
{
    internal static readonly int[] MonthsAhead = [1, 2];

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        bool partitioned = await maintainer
            .IsParentPartitionedAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!partitioned)
        {
            Log.TableNotPartitioned(logger);
            return;
        }

        DateTimeOffset now = clock.GetUtcNow();
        foreach (int monthsAhead in MonthsAhead)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DateTimeOffset target = now.AddMonths(monthsAhead);
            await maintainer
                .CreatePartitionAsync(target.Year, target.Month, cancellationToken)
                .ConfigureAwait(false);
            string name = $"iot_telemetry_points_{target.Year:D4}_{target.Month:D2}";
            metrics.RecordPartitionCreated(name);
            Log.PartitionEnsured(logger, name);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Warning,
            Message = "iot_telemetry_points is not partitioned — partition maintenance job skipped. Call MigrationBuilder.EnableTelemetryPartitioning() in a migration to opt in.")]
        public static partial void TableNotPartitioned(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Ensured monthly telemetry partition '{PartitionName}'.")]
        public static partial void PartitionEnsured(ILogger logger, string partitionName);
    }
}
