using Granit.IoT.Ingestion.Abstractions;

namespace Granit.IoT.Ingestion.Aws;

/// <summary>
/// Verifies an AWS Signature Version 4 request. Unlike
/// <see cref="Granit.IoT.Ingestion.Abstractions.IPayloadSignatureValidator"/>,
/// this contract requires the HTTP method, canonical URI, and canonical query
/// string to be passed in explicitly — they are part of the signed material
/// but are not available from <c>body + headers</c> alone.
/// </summary>
/// <remarks>
/// The endpoint layer (the Direct and API Gateway inbound routes) is
/// responsible for supplying the method, the path, and the canonical query
/// string. Callers must also enforce the 5-minute clock-skew tolerance on
/// <c>x-amz-date</c>; this validator does it centrally so every inbound path
/// inherits the behaviour.
/// </remarks>
public interface ISigV4RequestValidator
{
    /// <summary>
    /// Validates the SigV4 signature carried by the inbound request.
    /// </summary>
    /// <param name="method">HTTP method in uppercase (<c>POST</c>, <c>GET</c>).</param>
    /// <param name="canonicalUri">
    /// URI path as presented to the server, e.g. <c>/iot/ingest/awsiotdirect</c>.
    /// Must NOT be URL-decoded a second time — pass what ASP.NET Core gives via
    /// <c>HttpRequest.Path.Value</c> (already decoded once).
    /// </param>
    /// <param name="canonicalQueryString">
    /// Query string with keys URL-encoded, sorted alphabetically, joined by
    /// <c>&amp;</c> — or the empty string when there is no query. This matches
    /// the AWS canonical request format.
    /// </param>
    /// <param name="headers">
    /// All inbound headers (case-insensitive keys). Must include at minimum
    /// <c>host</c>, <c>x-amz-date</c>, and every entry listed in the
    /// <c>SignedHeaders</c> component of the <c>Authorization</c> header.
    /// </param>
    /// <param name="body">Raw request body bytes.</param>
    ValueTask<SignatureValidationResult> ValidateAsync(
        string method,
        string canonicalUri,
        string canonicalQueryString,
        IReadOnlyDictionary<string, string> headers,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken);
}
