using Granit.IoT.Extensions;
using Granit.Modularity;

namespace Granit.IoT;

/// <summary>
/// Root module of the Granit.IoT family — registers the domain abstractions
/// (<c>IDeviceReader</c>, <c>IDeviceWriter</c>, <c>ITelemetryReader</c>,
/// <c>ITelemetryWriter</c>), permission definitions, and diagnostics. All other
/// IoT modules depend on this one implicitly.
/// </summary>
public sealed class GranitIoTModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIoT();
}
