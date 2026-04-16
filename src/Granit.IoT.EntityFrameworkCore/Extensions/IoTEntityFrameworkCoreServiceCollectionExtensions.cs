using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.EntityFrameworkCore.Extensions;

public static class IoTEntityFrameworkCoreServiceCollectionExtensions
{
    public static IServiceCollection AddGranitIoTEntityFrameworkCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        services.AddGranitDbContext<IoTDbContext>(configure);

        services.TryAddScoped<IDeviceReader, DeviceEfCoreReader>();
        services.TryAddScoped<IDeviceWriter, DeviceEfCoreWriter>();
        services.TryAddScoped<ITelemetryReader, TelemetryEfCoreReader>();
        services.TryAddScoped<ITelemetryWriter, TelemetryEfCoreWriter>();

        return services;
    }
}
