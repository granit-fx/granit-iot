using Granit.BackgroundJobs;
using Granit.IoT.Abstractions;
using Granit.IoT.BackgroundJobs.Internal;
using Granit.IoT.BackgroundJobs.Services;
using Granit.IoT.Wolverine;
using Granit.Modularity;
using Granit.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.BackgroundJobs;

/// <summary>
/// Registers the IoT background-job services + a no-op partition maintainer
/// so the maintenance job runs cleanly even when no provider-specific
/// implementation is wired. The Postgres provider replaces the no-op via
/// <c>AddGranitIoTPostgres()</c>.
/// </summary>
[DependsOn(typeof(GranitBackgroundJobsModule))]
[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitIoTWolverineModule))]
[DependsOn(typeof(GranitSettingsModule))]
public sealed class GranitIoTBackgroundJobsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddMemoryCache();
        context.Services.TryAddSingleton<DeviceOfflineTrackerCache>();
        context.Services.TryAddTransient<StaleTelemetryPurgeService>();
        context.Services.TryAddTransient<DeviceHeartbeatTimeoutService>();
        context.Services.TryAddTransient<TelemetryPartitionMaintenanceService>();
        context.Services.TryAddSingleton<ITelemetryPartitionMaintainer, NoOpTelemetryPartitionMaintainer>();
    }
}
