namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Outcome of a single inbound webhook delivery as seen by the ingestion endpoint.
/// </summary>
public enum IngestionOutcome
{
    /// <summary>Payload accepted and dispatched to the outbox (or treated as a duplicate).</summary>
    Accepted,

    /// <summary>Signature validation failed — pipeline returns <c>401 Unauthorized</c>.</summary>
    SignatureRejected,

    /// <summary>No parser is registered for the requested source.</summary>
    UnknownSource,

    /// <summary>Payload could not be parsed — pipeline returns <c>400 Bad Request</c>.</summary>
    ParseFailure,
}

/// <summary>
/// Result of an ingestion attempt. <see cref="Reason"/> is populated for non-accepted outcomes.
/// </summary>
public sealed record IngestionResult(IngestionOutcome Outcome, string? Reason = null)
{
    public static IngestionResult Accepted { get; } = new(IngestionOutcome.Accepted);

    public static IngestionResult SignatureRejected(string reason) =>
        new(IngestionOutcome.SignatureRejected, reason);

    public static IngestionResult UnknownSource(string source) =>
        new(IngestionOutcome.UnknownSource, $"No ingestion provider registered for source '{source}'.");

    public static IngestionResult ParseFailure(string reason) =>
        new(IngestionOutcome.ParseFailure, reason);
}
