using Granit.IoT.Ingestion;
using Granit.Modularity;

namespace Granit.IoT.Ingestion.Endpoints;

[DependsOn(typeof(GranitIoTIngestionModule))]
public sealed class GranitIoTIngestionEndpointsModule : GranitModule;
