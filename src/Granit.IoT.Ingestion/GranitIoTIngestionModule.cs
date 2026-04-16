using Granit.IoT.Ingestion.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Ingestion;

[DependsOn(typeof(GranitIoTModule))]
public sealed class GranitIoTIngestionModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIoTIngestion(context.Builder.Environment);
}
