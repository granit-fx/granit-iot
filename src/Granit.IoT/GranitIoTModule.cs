using Granit.IoT.Extensions;
using Granit.Modularity;

namespace Granit.IoT;

public sealed class GranitIoTModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIoT();
}
