using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws.Diagnostics;
using Granit.IoT.Ingestion.Aws.Internal;
using Granit.IoT.Ingestion.Aws.Internal.SigV4;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Aws.Extensions;

/// <summary>
/// DI wiring for the AWS IoT Core ingestion provider.
/// </summary>
public static class IoTIngestionAwsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AWS IoT ingestion options, the SNS signing-cert cache, the
    /// SNS signature validator, and the per-path metrics. Idempotent.
    /// SigV4 validators and parsers are registered by follow-up modules.
    /// </summary>
    public static IServiceCollection AddGranitIoTIngestionAws(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AwsIoTIngestionOptions>()
            .BindConfiguration(AwsIoTIngestionOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AwsIoTIngestionOptions>, AwsIoTIngestionOptionsValidator>());

        services.AddHttpClient(
            DefaultSnsSigningCertificateCache.HttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddHttpClient(
            SnsPayloadSignatureValidator.SubscribeHttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(10));

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IoTIngestionAwsMetrics>();
        services.TryAddSingleton<ISnsSigningCertificateCache, DefaultSnsSigningCertificateCache>();
        services.AddSingleton<IPayloadSignatureValidator, SnsPayloadSignatureValidator>();

        services.TryAddSingleton<ISigV4RequestValidator, DefaultSigV4RequestValidator>();
        services.AddSingleton<IPayloadSignatureValidator, DirectPayloadSignatureValidator>();
        services.AddSingleton<IPayloadSignatureValidator, ApiGatewayPayloadSignatureValidator>();

        services.AddSingleton<IInboundMessageParser>(_ =>
            new AwsIoTRulePayloadParser(AwsIoTIngestionConstants.DirectSourceName));
        services.AddSingleton<IInboundMessageParser>(_ =>
            new AwsIoTRulePayloadParser(AwsIoTIngestionConstants.ApiGatewaySourceName));
        services.AddSingleton<IInboundMessageParser, AwsIoTSnsPayloadParser>();

        return services;
    }
}
