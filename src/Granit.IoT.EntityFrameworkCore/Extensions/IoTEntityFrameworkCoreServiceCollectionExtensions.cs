using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.EntityFrameworkCore.Extensions;

/// <summary>
/// Service-collection extensions for the IoT EF Core layer
/// (<c>Granit.IoT.EntityFrameworkCore</c>).
/// </summary>
public static class IoTEntityFrameworkCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the isolated <c>IoTDbContext</c> (via
    /// <c>AddGranitDbContext</c>) and the EF Core implementations of
    /// <c>IDeviceReader</c>/<c>IDeviceWriter</c>/<c>IDeviceLookup</c>/<c>ITelemetryReader</c>/<c>ITelemetryWriter</c>/<c>ITelemetryPurger</c>.
    /// Idempotent via <c>TryAdd*</c>.
    /// </summary>
    public static IServiceCollection AddGranitIoTEntityFrameworkCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        services.AddGranitDbContext<IoTDbContext>(configure);

        services.TryAddScoped<IDeviceReader, DeviceEfCoreReader>();
        services.TryAddScoped<IDeviceWriter, DeviceEfCoreWriter>();
        services.TryAddScoped<IDeviceLookup, DeviceLookupEfCore>();
        services.TryAddScoped<ITelemetryReader, TelemetryEfCoreReader>();
        services.TryAddScoped<ITelemetryWriter, TelemetryEfCoreWriter>();
        services.TryAddScoped<ITelemetryPurger, TelemetryEfCorePurger>();

        return services;
    }
}
