using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Postgres.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Extensions;

public static class IoTPostgresServiceCollectionExtensions
{
    /// <summary>
    /// Registers PostgreSQL-specific runtime services for the IoT module.
    /// Currently swaps in <see cref="PostgresTelemetryPartitionMaintainer"/>
    /// so <c>TelemetryPartitionMaintenanceJob</c> can create future monthly
    /// partitions. Without this call the job runs against the no-op maintainer
    /// registered by <c>Granit.IoT.BackgroundJobs</c> and exits without work.
    /// </summary>
    public static IServiceCollection AddGranitIoTPostgres(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.RemoveAll<ITelemetryPartitionMaintainer>();
        services.AddScoped<ITelemetryPartitionMaintainer, PostgresTelemetryPartitionMaintainer>();
        return services;
    }
}
