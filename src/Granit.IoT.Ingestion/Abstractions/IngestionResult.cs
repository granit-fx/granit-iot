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
/// <param name="Outcome">Accepted / SignatureRejected / UnknownSource / ParseFailure.</param>
/// <param name="Reason">Human-readable explanation surfaced in <c>ProblemDetails.detail</c> for non-accepted outcomes.</param>
public sealed record IngestionResult(IngestionOutcome Outcome, string? Reason = null)
{
    /// <summary>Singleton result for the successful ingestion path.</summary>
    public static IngestionResult Accepted { get; } = new(IngestionOutcome.Accepted);

    /// <summary>Factory for <see cref="IngestionOutcome.SignatureRejected"/> outcomes with the given reason.</summary>
    public static IngestionResult SignatureRejected(string reason) =>
        new(IngestionOutcome.SignatureRejected, reason);

    /// <summary>Factory for <see cref="IngestionOutcome.UnknownSource"/> outcomes tagged with the source discriminator that was requested.</summary>
    public static IngestionResult UnknownSource(string source) =>
        new(IngestionOutcome.UnknownSource, $"No ingestion provider registered for source '{source}'.");

    /// <summary>Factory for <see cref="IngestionOutcome.ParseFailure"/> outcomes with the given reason.</summary>
    public static IngestionResult ParseFailure(string reason) =>
        new(IngestionOutcome.ParseFailure, reason);
}
