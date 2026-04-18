using System.Text;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Endpoints.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Endpoints;

public sealed class IngestionEndpointsTests
{
    [Fact]
    public async Task IngestAsync_WrongContentType_Returns415()
    {
        IIngestionPipeline pipeline = Substitute.For<IIngestionPipeline>();
        HttpContext ctx = NewContext("text/plain", body: "{}");

        IResult result = await IngestionEndpoints.IngestAsync(
            "scaleway", ctx.Request, pipeline, TestContext.Current.CancellationToken).ConfigureAwait(true);

        ProblemHttpResult prob = result.ShouldBeOfType<ProblemHttpResult>();
        prob.StatusCode.ShouldBe(StatusCodes.Status415UnsupportedMediaType);
        await pipeline.DidNotReceiveWithAnyArgs()
            .ProcessAsync(default!, default, default!, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task IngestAsync_NullContentType_Returns415()
    {
        IIngestionPipeline pipeline = Substitute.For<IIngestionPipeline>();
        HttpContext ctx = NewContext(contentType: null, body: "{}");

        IResult result = await IngestionEndpoints.IngestAsync(
            "scaleway", ctx.Request, pipeline, TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status415UnsupportedMediaType);
    }

    [Fact]
    public async Task IngestAsync_Accepted_Returns202()
    {
        IIngestionPipeline pipeline = Substitute.For<IIngestionPipeline>();
        pipeline.ProcessAsync(
                Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(IngestionResult.Accepted);

        HttpContext ctx = NewContext("application/json", body: "{\"x\":1}");

        IResult result = await IngestionEndpoints.IngestAsync(
            "scaleway", ctx.Request, pipeline, TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.ShouldBeOfType<Accepted>();
    }

    [Fact]
    public async Task IngestAsync_SignatureRejected_Returns401()
    {
        IIngestionPipeline pipeline = Substitute.For<IIngestionPipeline>();
        pipeline.ProcessAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(IngestionResult.SignatureRejected("bad sig"));

        HttpContext ctx = NewContext("application/json", body: "{}");

        IResult result = await IngestionEndpoints.IngestAsync(
            "scaleway", ctx.Request, pipeline, TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task IngestAsync_UnknownSource_Returns422()
    {
        IIngestionPipeline pipeline = Substitute.For<IIngestionPipeline>();
        pipeline.ProcessAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(IngestionResult.UnknownSource("xyz"));

        HttpContext ctx = NewContext("application/json", body: "{}");

        IResult result = await IngestionEndpoints.IngestAsync(
            "xyz", ctx.Request, pipeline, TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task IngestAsync_ParseFailure_Returns400()
    {
        IIngestionPipeline pipeline = Substitute.For<IIngestionPipeline>();
        pipeline.ProcessAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(IngestionResult.ParseFailure("bad json"));

        HttpContext ctx = NewContext("application/json", body: "{}");

        IResult result = await IngestionEndpoints.IngestAsync(
            "scaleway", ctx.Request, pipeline, TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task IngestAsync_StripsClientGranitRequestHeaders_AndInjectsServerHeaders()
    {
        IIngestionPipeline pipeline = Substitute.For<IIngestionPipeline>();
        IReadOnlyDictionary<string, string>? captured = null;
        pipeline.ProcessAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Do<IReadOnlyDictionary<string, string>>(h => captured = h), Arg.Any<CancellationToken>())
            .Returns(IngestionResult.Accepted);

        HttpContext ctx = NewContext("application/json", body: "{}", method: "POST", path: "/iot/ingest/scaleway", queryString: "?x=1");
        ctx.Request.Headers["granit-request-method"] = "GET";
        ctx.Request.Headers["X-Custom"] = "kept";

        await IngestionEndpoints.IngestAsync(
            "scaleway", ctx.Request, pipeline, TestContext.Current.CancellationToken).ConfigureAwait(true);

        captured.ShouldNotBeNull();
        captured["granit-request-method"].ShouldBe("POST");
        captured["granit-request-path"].ShouldBe("/iot/ingest/scaleway");
        captured["granit-request-query"].ShouldBe("x=1");
        captured["X-Custom"].ShouldBe("kept");
    }

    private static DefaultHttpContext NewContext(
        string? contentType,
        string body,
        string method = "POST",
        string path = "/iot/ingest/scaleway",
        string queryString = "")
    {
        DefaultHttpContext ctx = new();
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        ctx.Request.QueryString = new QueryString(queryString);
        ctx.Request.ContentType = contentType;
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        ctx.Request.ContentLength = ctx.Request.Body.Length;
        return ctx;
    }
}
