using Granit.Caching;
using Granit.IoT.Ingestion.Aws.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Ingestion.Aws;

/// <summary>
/// AWS IoT Core ingestion provider. Wires the SNS signature validator, cert
/// cache, and per-path metrics into the provider-agnostic
/// <c>POST /iot/ingest/{source}</c> pipeline from
/// <see cref="GranitIoTIngestionModule"/>. SigV4 (Direct / API Gateway) and
/// the message parsers land in follow-up commits.
/// </summary>
[DependsOn(typeof(GranitIoTIngestionModule))]
[DependsOn(typeof(GranitCachingModule))]
public sealed class GranitIoTIngestionAwsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTIngestionAws();
    }
}
