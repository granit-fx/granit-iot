using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Timescale.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.EntityFrameworkCore.Timescale.Extensions;

/// <summary>
/// DI registration for <c>Granit.IoT.EntityFrameworkCore.Timescale</c>. Replaces
/// the default <c>ITelemetryReader</c> with the TimescaleDB-aware reader.
/// </summary>
public static class IoTTimescaleServiceCollectionExtensions
{
    /// <summary>
    /// Swaps the registered <see cref="ITelemetryReader"/> for one that routes
    /// aggregate queries through TimescaleDB continuous aggregates when the
    /// requested window justifies it. Idempotent — calling it twice does not
    /// register the reader twice.
    /// </summary>
    public static IServiceCollection AddGranitIoTTimescale(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<ITelemetryReader>();
        services.AddScoped<ITelemetryReader, TimescaleTelemetryEfCoreReader>();
        return services;
    }
}
