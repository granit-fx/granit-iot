namespace Granit.IoT.Ingestion;

/// <summary>
/// Raised by <see cref="Abstractions.IInboundMessageParser"/> implementations when an
/// inbound payload cannot be decoded into a <see cref="Abstractions.ParsedTelemetryBatch"/>.
/// </summary>
public sealed class IngestionParseException : Exception
{
    /// <summary>Initializes a new instance with the given message.</summary>
    public IngestionParseException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    public IngestionParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
