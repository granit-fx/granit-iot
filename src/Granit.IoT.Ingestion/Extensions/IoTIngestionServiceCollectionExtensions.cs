using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Internal;
using Granit.IoT.Ingestion.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.IoT.Ingestion.Extensions;

/// <summary>
/// Service-collection extensions for the provider-agnostic IoT ingestion
/// pipeline (<c>Granit.IoT.Ingestion</c>).
/// </summary>
public static class IoTIngestionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ingestion pipeline, the transport-level deduplicator
    /// and (in Development only) the permissive
    /// <c>NullPayloadSignatureValidator</c>. Idempotent via <c>TryAdd*</c>.
    /// </summary>
    public static IServiceCollection AddGranitIoTIngestion(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddOptions<GranitIoTIngestionOptions>()
            .BindConfiguration(GranitIoTIngestionOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddScoped<IIngestionPipeline, IngestionPipeline>();
        services.TryAddScoped<IInboundMessageDeduplicator, IdempotencyStoreInboundMessageDeduplicator>();

        if (environment.IsDevelopment())
        {
            services.AddSingleton<IPayloadSignatureValidator, NullPayloadSignatureValidator>();
        }

        return services;
    }
}
