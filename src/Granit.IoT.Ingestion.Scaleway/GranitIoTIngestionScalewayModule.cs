using Granit.IoT.Ingestion;
using Granit.Modularity;

namespace Granit.IoT.Ingestion.Scaleway;

[DependsOn(typeof(GranitIoTIngestionModule))]
public sealed class GranitIoTIngestionScalewayModule : GranitModule;
