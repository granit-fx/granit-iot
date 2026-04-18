using Granit.IoT.Wolverine.Abstractions;
using Granit.IoT.Wolverine.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Wolverine.Extensions;

/// <summary>
/// Service-collection extensions for the Wolverine message-handler satellite
/// (<c>Granit.IoT.Wolverine</c>).
/// </summary>
public static class IoTWolverineServiceCollectionExtensions
{
    /// <summary>
    /// Registers the settings-backed <c>IDeviceThresholdEvaluator</c> used by
    /// <c>TelemetryIngestedHandler</c> to decide which metrics breach a
    /// tenant-configured threshold. Idempotent via <c>TryAdd*</c>.
    /// </summary>
    public static IServiceCollection AddGranitIoTWolverine(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDeviceThresholdEvaluator, SettingsDeviceThresholdEvaluator>();

        return services;
    }
}
