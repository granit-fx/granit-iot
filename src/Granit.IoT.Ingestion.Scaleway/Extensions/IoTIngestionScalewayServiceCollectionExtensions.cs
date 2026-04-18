using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Scaleway.Internal;
using Granit.IoT.Ingestion.Scaleway.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Scaleway.Extensions;

/// <summary>
/// Service-collection extensions for the Scaleway IoT Hub ingestion provider
/// (<c>Granit.IoT.Ingestion.Scaleway</c>).
/// </summary>
public static class IoTIngestionScalewayServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Scaleway provider: HMAC-SHA256 signature validator,
    /// topic mapper, and message parser. Binds
    /// <see cref="ScalewayIoTOptions"/> with validation on startup.
    /// </summary>
    public static IServiceCollection AddGranitIoTIngestionScaleway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ScalewayIoTOptions>()
            .BindConfiguration(ScalewayIoTOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ScalewayIoTOptions>, ScalewayIoTOptionsValidator>());

        services.TryAddSingleton<ScalewayTopicMapper>();
        services.AddSingleton<IPayloadSignatureValidator, ScalewaySignatureValidator>();
        services.AddSingleton<IInboundMessageParser, ScalewayMessageParser>();

        return services;
    }
}
