using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Internal;
using Granit.IoT.Ingestion.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        services.AddOptions<GranitIoTIngestionOptions>()
            .BindConfiguration(GranitIoTIngestionOptions.SectionName)
            .ValidateDataAnnotations();

        services.TryAddScoped<IIngestionPipeline, IngestionPipeline>();
        services.TryAddScoped<IInboundMessageDeduplicator, IdempotencyStoreInboundMessageDeduplicator>();

        if (environment.IsDevelopment())
        {
            services.AddSingleton<IPayloadSignatureValidator, NullPayloadSignatureValidator>();
        }

        return services;
    }
}
