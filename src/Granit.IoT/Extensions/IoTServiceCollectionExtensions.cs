using Granit.Diagnostics;
using Granit.IoT.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Extensions;

public static class IoTServiceCollectionExtensions
{
    public static IServiceCollection AddGranitIoT(this IServiceCollection services)
    {
        GranitActivitySourceRegistry.Register(IoTActivitySource.Name);
        services.TryAddSingleton<IoTMetrics>();

        return services;
    }
}
