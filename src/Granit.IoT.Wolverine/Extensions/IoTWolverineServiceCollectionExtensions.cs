using Granit.IoT.Wolverine.Abstractions;
using Granit.IoT.Wolverine.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Wolverine.Extensions;

public static class IoTWolverineServiceCollectionExtensions
{
    public static IServiceCollection AddGranitIoTWolverine(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDeviceThresholdEvaluator, SettingsDeviceThresholdEvaluator>();

        return services;
    }
}
