using Granit.IoT.Ingestion.Scaleway.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Ingestion.Scaleway;

/// <summary>
/// Scaleway IoT Hub ingestion provider. Registers the HMAC-SHA256 signature validator,
/// topic mapper, and message parser for payloads delivered via the Scaleway HTTP forwarder.
/// </summary>
[DependsOn(typeof(GranitIoTIngestionModule))]
public sealed class GranitIoTIngestionScalewayModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIoTIngestionScaleway();
}
