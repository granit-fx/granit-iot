using Granit.IoT.Aws.Shadow.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Aws.Shadow;

/// <summary>
/// Bidirectional Device Shadow bridge module. Pushes status updates to the
/// reported document on every IoT lifecycle transition and polls the
/// desired/reported delta to surface cloud-issued state changes as
/// <c>DeviceDesiredStateChangedEvent</c>. The matching command dispatcher
/// (PR #6, story #49) listens for that event.
/// </summary>
[DependsOn(typeof(GranitIoTAwsModule))]
public sealed class GranitIoTAwsShadowModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTAwsShadow();
    }
}
