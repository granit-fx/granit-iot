using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Scaleway.Internal;
using Granit.IoT.Ingestion.Scaleway.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Ingestion.Scaleway.Extensions;

public static class IoTIngestionScalewayServiceCollectionExtensions
{
    public static IServiceCollection AddGranitIoTIngestionScaleway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ScalewayIoTOptions>()
            .BindConfiguration(ScalewayIoTOptions.SectionName)
            .ValidateDataAnnotations();

        services.TryAddSingleton<ScalewayTopicMapper>();
        services.AddSingleton<IPayloadSignatureValidator, ScalewaySignatureValidator>();
        services.AddSingleton<IInboundMessageParser, ScalewayMessageParser>();

        return services;
    }
}
