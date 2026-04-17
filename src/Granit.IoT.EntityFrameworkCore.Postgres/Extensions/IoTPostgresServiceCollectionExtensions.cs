using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Postgres.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Extensions;

public static class IoTPostgresServiceCollectionExtensions
{
    /// <summary>
    /// Registers PostgreSQL-specific runtime services for the IoT module:
    /// <list type="bullet">
    /// <item><see cref="PostgresTelemetryPartitionMaintainer"/> so
    /// <c>TelemetryPartitionMaintenanceJob</c> can create future monthly partitions.
    /// Without this call the job runs against the no-op maintainer registered by
    /// <c>Granit.IoT.BackgroundJobs</c> and exits without work.</item>
    /// <item><see cref="PostgresTelemetryEfCoreReader"/> which enables metric-level
    /// <c>Avg</c>/<c>Min</c>/<c>Max</c> aggregation via JSONB extraction. The generic
    /// reader only supports <c>Count</c> and throws <see cref="NotSupportedException"/>
    /// for other aggregations.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddGranitIoTPostgres(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<ITelemetryPartitionMaintainer>();
        services.AddScoped<ITelemetryPartitionMaintainer, PostgresTelemetryPartitionMaintainer>();

        services.RemoveAll<ITelemetryReader>();
        services.AddScoped<ITelemetryReader, PostgresTelemetryEfCoreReader>();

        return services;
    }
}
