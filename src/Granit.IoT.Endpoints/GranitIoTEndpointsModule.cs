using Granit.Modularity;

namespace Granit.IoT.Endpoints;

[DependsOn(typeof(GranitIoTModule))]
public sealed class GranitIoTEndpointsModule : GranitModule;
