namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Orchestrates inbound webhook processing: signature validation → parsing → deduplication
/// → device resolution → outbox dispatch. Implementations must complete in well under
/// the IoT-hub HTTP response budget (~2s) and never perform synchronous DB writes.
/// </summary>
public interface IIngestionPipeline
{
    /// <summary>
    /// Processes a single inbound payload from the named provider source.
    /// </summary>
    /// <param name="source">Provider source key matching <see cref="IInboundMessageParser.SourceName"/>.</param>
    /// <param name="body">Raw HTTP body bytes.</param>
    /// <param name="headers">Inbound request headers (case-insensitive).</param>
    Task<IngestionResult> ProcessAsync(
        string source,
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}
