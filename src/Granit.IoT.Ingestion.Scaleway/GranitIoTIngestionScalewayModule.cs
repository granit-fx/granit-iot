using Granit.IoT.Ingestion.Scaleway.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Ingestion.Scaleway;

[DependsOn(typeof(GranitIoTIngestionModule))]
public sealed class GranitIoTIngestionScalewayModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIoTIngestionScaleway();
}
