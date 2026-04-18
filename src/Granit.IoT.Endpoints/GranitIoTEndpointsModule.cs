using Granit.Modularity;

namespace Granit.IoT.Endpoints;

/// <summary>
/// Minimal API route groups for IoT device management: <c>/api/iot/devices</c> and
/// <c>/api/iot/telemetry</c>. Host wiring goes through <c>MapGranitIoTEndpoints()</c>.
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
public sealed class GranitIoTEndpointsModule : GranitModule;
