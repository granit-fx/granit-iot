using Granit.Events.Wolverine;
using Granit.IoT.EntityFrameworkCore;
using Granit.IoT.Ingestion;
using Granit.IoT.Wolverine.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Wolverine;

/// <summary>
/// Wolverine message handlers for IoT: <c>TelemetryIngestedHandler</c> persists readings
/// and evaluates thresholds after commit, <c>DeviceLifecycleHandlers</c> bridge domain
/// events into the distributed outbox. Idempotent / retry-safe per CLAUDE.md §9d.
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitIoTEntityFrameworkCoreModule))]
[DependsOn(typeof(GranitIoTIngestionModule))]
[DependsOn(typeof(GranitEventsWolverineModule))]
public sealed class GranitIoTWolverineModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIoTWolverine();
}
