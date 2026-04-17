namespace Granit.IoT.Ingestion.Abstractions;

/// <summary>
/// Names of server-controlled synthetic headers the ingestion endpoint adds
/// before invoking <see cref="IPayloadSignatureValidator"/>. These carry HTTP
/// request metadata that is not otherwise reachable from the <c>body</c> /
/// <c>headers</c> pair — specifically the method, path, and query string
/// needed to reconstruct a SigV4 canonical request.
/// </summary>
/// <remarks>
/// These headers are ALWAYS stripped from inbound traffic and reset from the
/// server's <see cref="Microsoft.AspNetCore.Http.HttpRequest"/> by the
/// endpoint layer. Never trust a <c>granit-request-*</c> header sent by a
/// client — its value was overwritten before it reached the validator.
/// </remarks>
public static class IngestionRequestHeaders
{
    /// <summary>Prefix used by all server-controlled synthetic headers.</summary>
    public const string ServerControlledPrefix = "granit-request-";

    /// <summary>HTTP method in upper case (e.g. <c>POST</c>).</summary>
    public const string Method = "granit-request-method";

    /// <summary>URI path as seen by ASP.NET Core (e.g. <c>/iot/ingest/awsiotdirect</c>).</summary>
    public const string Path = "granit-request-path";

    /// <summary>
    /// Canonical query string (URL-encoded keys, sorted alphabetically) or
    /// empty when the request has no query. Matches the AWS SigV4 shape.
    /// </summary>
    public const string Query = "granit-request-query";
}
