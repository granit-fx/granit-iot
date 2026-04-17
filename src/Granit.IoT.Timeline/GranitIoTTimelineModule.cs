using Granit.Modularity;
using Granit.Timeline;

namespace Granit.IoT.Timeline;

/// <summary>
/// Bridge module: subscribes to device lifecycle domain events and writes
/// immutable Granit.Timeline entries (entity type "Device"). Provides the
/// ISO 27001 audit trail for device provisioning, activation, suspension,
/// reactivation, and decommissioning. Wolverine discovers the static
/// <c>DeviceTimelineHandler</c> automatically — no DI registration is
/// required beyond depending on this module.
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitTimelineModule))]
public sealed class GranitIoTTimelineModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        // No services to register: handler is static, ITimelineWriter comes from GranitTimelineModule.
    }
}
