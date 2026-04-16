using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Granit.IoT.Ingestion.Extensions;

public static class IoTIngestionServiceCollectionExtensions
{
    public static IServiceCollection AddGranitIoTIngestion(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        return services;
    }
}
