namespace Granit.IoT.Ingestion;

/// <summary>
/// Raised by <see cref="Abstractions.IInboundMessageParser"/> implementations when an
/// inbound payload cannot be decoded into a <see cref="Abstractions.ParsedTelemetryBatch"/>.
/// </summary>
public sealed class IngestionParseException : Exception
{
    public IngestionParseException(string message) : base(message)
    {
    }

    public IngestionParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
