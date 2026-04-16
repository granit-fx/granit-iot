using Granit.IoT.Diagnostics;
using Granit.IoT.Ingestion.Abstractions;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Granit.IoT.Ingestion.Endpoints.Endpoints;

internal static class IngestionEndpoints
{
    private const string JsonContentType = "application/json";
    private const int MaxBodySizeBytes = 256 * 1024;

    internal static RouteGroupBuilder MapIngestionRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/{source}", IngestAsync)
            .WithMetadata(new SkipAutoValidationAttribute())
            .WithName("IngestTelemetry")
            .WithSummary("Receives a signed telemetry webhook from an IoT hub provider.")
            .WithDescription("Validates the provider signature, deduplicates by transport message id, resolves the device, and dispatches to the Wolverine outbox. Returns 202 Accepted with no synchronous DB write.")
            .Accepts<object>(JsonContentType)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }

    private static async Task<IResult> IngestAsync(
        string source,
        HttpRequest request,
        [FromServices] IIngestionPipeline pipeline,
        [FromServices] IoTMetrics metrics,
        CancellationToken cancellationToken)
    {
        if (!IsJsonContentType(request.ContentType))
        {
            return TypedResults.Problem(
                detail: $"Content-Type '{request.ContentType}' is not supported. Expected '{JsonContentType}'.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        request.EnableBuffering(bufferThreshold: MaxBodySizeBytes, bufferLimit: MaxBodySizeBytes);

        using MemoryStream memory = new();
        await request.Body.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        ReadOnlyMemory<byte> body = memory.ToArray();

        Dictionary<string, string> headers = SnapshotHeaders(request.Headers);

        IngestionResult result = await pipeline
            .ProcessAsync(source, body, headers, cancellationToken)
            .ConfigureAwait(false);

        return result.Outcome switch
        {
            IngestionOutcome.Accepted => TypedResults.Accepted((string?)null),
            IngestionOutcome.SignatureRejected => TypedResults.Problem(
                detail: result.Reason,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid signature"),
            IngestionOutcome.UnknownSource => TypedResults.Problem(
                detail: result.Reason,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Unknown ingestion source"),
            IngestionOutcome.ParseFailure => TypedResults.Problem(
                detail: result.Reason,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Malformed payload"),
            _ => TypedResults.Problem(
                detail: "Unexpected ingestion outcome.",
                statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? parsed)
            && string.Equals(parsed.MediaType.Value, JsonContentType, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> SnapshotHeaders(IHeaderDictionary headers)
    {
        Dictionary<string, string> snapshot = new(headers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in headers)
        {
            snapshot[header.Key] = header.Value.ToString();
        }

        return snapshot;
    }
}
