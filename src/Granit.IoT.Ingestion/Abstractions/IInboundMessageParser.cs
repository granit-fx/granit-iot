namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Parses a provider-specific webhook payload into a normalized <see cref="ParsedTelemetryBatch"/>.
/// Implementations are resolved by matching <see cref="SourceName"/> to the route segment
/// <c>{source}</c> of the ingestion endpoint.
/// </summary>
public interface IInboundMessageParser
{
    /// <summary>
    /// Provider source key (e.g. <c>"scaleway"</c>). Must be lowercase.
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Parses the raw HTTP body bytes into a normalized batch.
    /// </summary>
    /// <exception cref="IngestionParseException">
    /// Thrown when the payload cannot be decoded or routed to a device.
    /// </exception>
    ValueTask<ParsedTelemetryBatch> ParseAsync(
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken);
}
