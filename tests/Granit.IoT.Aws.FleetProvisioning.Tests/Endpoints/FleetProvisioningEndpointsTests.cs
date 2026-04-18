using Granit.IoT.Aws.FleetProvisioning.Abstractions;
using Granit.IoT.Aws.FleetProvisioning.Contracts;
using Granit.IoT.Aws.FleetProvisioning.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Granit.IoT.Aws.FleetProvisioning.Tests.Endpoints;

public sealed class FleetProvisioningEndpointsTests
{
    [Fact]
    public async Task VerifyAsync_NullRequest_Returns400()
    {
        IFleetProvisioningService service = Substitute.For<IFleetProvisioningService>();

        Results<Ok<FleetProvisioningVerifyResponse>, ProblemHttpResult> result = await FleetProvisioningEndpoints
            .VerifyAsync(null!, service, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task VerifyAsync_BlankSerial_Returns400()
    {
        IFleetProvisioningService service = Substitute.For<IFleetProvisioningService>();
        FleetProvisioningVerifyRequest req = new("   ", null);

        Results<Ok<FleetProvisioningVerifyResponse>, ProblemHttpResult> result = await FleetProvisioningEndpoints
            .VerifyAsync(req, service, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task VerifyAsync_Allowed_Returns200WithResponse()
    {
        IFleetProvisioningService service = Substitute.For<IFleetProvisioningService>();
        FleetProvisioningVerifyRequest req = new("SN-1", null);
        service.VerifyAsync(req, Arg.Any<CancellationToken>())
            .Returns(new FleetProvisioningVerifyResponse(true, null));

        Results<Ok<FleetProvisioningVerifyResponse>, ProblemHttpResult> result = await FleetProvisioningEndpoints
            .VerifyAsync(req, service, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Ok<FleetProvisioningVerifyResponse> ok = result.Result.ShouldBeOfType<Ok<FleetProvisioningVerifyResponse>>();
        ok.Value!.AllowProvisioning.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterAsync_NullRequest_Returns400()
    {
        IFleetProvisioningService service = Substitute.For<IFleetProvisioningService>();

        Results<Ok<FleetProvisioningRegisterResponse>, ProblemHttpResult> result = await FleetProvisioningEndpoints
            .RegisterAsync(null!, service, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Theory]
    [InlineData("", "T", "arn:t", "arn:c", "arn:s", "M", "1.0")]
    [InlineData("SN", "", "arn:t", "arn:c", "arn:s", "M", "1.0")]
    [InlineData("SN", "T", "", "arn:c", "arn:s", "M", "1.0")]
    [InlineData("SN", "T", "arn:t", "", "arn:s", "M", "1.0")]
    [InlineData("SN", "T", "arn:t", "arn:c", "", "M", "1.0")]
    [InlineData("SN", "T", "arn:t", "arn:c", "arn:s", "", "1.0")]
    [InlineData("SN", "T", "arn:t", "arn:c", "arn:s", "M", "")]
    public async Task RegisterAsync_AnyMissingField_Returns400(
        string serial, string thingName, string thingArn, string certArn,
        string certSecretArn, string model, string firmware)
    {
        IFleetProvisioningService service = Substitute.For<IFleetProvisioningService>();
        FleetProvisioningRegisterRequest req = new(
            serial, null, thingName, thingArn, certArn, certSecretArn, model, firmware, null, null);

        Results<Ok<FleetProvisioningRegisterResponse>, ProblemHttpResult> result = await FleetProvisioningEndpoints
            .RegisterAsync(req, service, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task RegisterAsync_DomainArgumentException_Returns422()
    {
        IFleetProvisioningService service = Substitute.For<IFleetProvisioningService>();
        FleetProvisioningRegisterRequest req = ValidRequest();
        service.RegisterAsync(req, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("invalid thing name"));

        Results<Ok<FleetProvisioningRegisterResponse>, ProblemHttpResult> result = await FleetProvisioningEndpoints
            .RegisterAsync(req, service, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task RegisterAsync_Success_Returns200()
    {
        IFleetProvisioningService service = Substitute.For<IFleetProvisioningService>();
        FleetProvisioningRegisterRequest req = ValidRequest();
        var id = Guid.NewGuid();
        service.RegisterAsync(req, Arg.Any<CancellationToken>())
            .Returns(new FleetProvisioningRegisterResponse(id, false));

        Results<Ok<FleetProvisioningRegisterResponse>, ProblemHttpResult> result = await FleetProvisioningEndpoints
            .RegisterAsync(req, service, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Ok<FleetProvisioningRegisterResponse> ok = result.Result.ShouldBeOfType<Ok<FleetProvisioningRegisterResponse>>();
        ok.Value!.DeviceId.ShouldBe(id);
        ok.Value.AlreadyProvisioned.ShouldBeFalse();
    }

    private static FleetProvisioningRegisterRequest ValidRequest() => new(
        "SN-1", null, "thing-1", "arn:aws:iot:thing/thing-1",
        "arn:aws:iot:cert/abc", "arn:aws:secretsmanager:secret/abc",
        "Model", "1.0.0", null, null);
}
