namespace Granit.IoT.Ingestion.Aws;

/// <summary>
/// Thrown when <see cref="ISnsSigningCertificateCache"/> cannot fetch the AWS
/// SNS signing certificate — the endpoint layer maps this to a <c>503</c>
/// response so the caller knows to retry with backoff.
/// </summary>
/// <remarks>
/// Do NOT swallow this exception; a persistent fetch failure usually indicates
/// an AWS incident or a network egress misconfiguration and must surface to
/// observability so ops can act.
/// </remarks>
public sealed class SnsSigningCertFetchException : Exception
{
    /// <summary>Initializes a new instance with the given message.</summary>
    public SnsSigningCertFetchException(string message) : base(message) { }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    public SnsSigningCertFetchException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Initializes a new instance with a default message.</summary>
    public SnsSigningCertFetchException() { }
}
