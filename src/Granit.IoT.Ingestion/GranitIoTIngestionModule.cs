using Granit.IoT.Ingestion.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Ingestion;

/// <summary>
/// Provider-agnostic telemetry ingestion pipeline: signature validation, transport-level
/// deduplication (Redis, 5-minute TTL), message parsing, and Wolverine outbox dispatch.
/// Provider-specific parsers (Scaleway, AWS, MQTT) plug in through <c>IInboundMessageParser</c>.
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
public sealed class GranitIoTIngestionModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIoTIngestion(context.Builder.Environment);
}
