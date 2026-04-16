using Granit.Events.Wolverine;
using Granit.IoT.EntityFrameworkCore;
using Granit.IoT.Ingestion;
using Granit.IoT.Wolverine.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Wolverine;

[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitIoTEntityFrameworkCoreModule))]
[DependsOn(typeof(GranitIoTIngestionModule))]
[DependsOn(typeof(GranitEventsWolverineModule))]
public sealed class GranitIoTWolverineModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIoTWolverine();
}
