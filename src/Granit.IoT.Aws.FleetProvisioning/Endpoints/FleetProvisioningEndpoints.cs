using Granit.IoT.Aws.FleetProvisioning.Abstractions;
using Granit.IoT.Aws.FleetProvisioning.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Granit.IoT.Aws.FleetProvisioning.Endpoints;

internal static class FleetProvisioningEndpoints
{
    internal static RouteGroupBuilder MapFleetProvisioningRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/verify", VerifyAsync)
            .WithName("FleetProvisioningVerify")
            .WithSummary("Pre-provisioning hook called by the customer's AWS Lambda before AWS IoT issues an operational certificate.")
            .WithDescription("Returns allowProvisioning=false when the serial belongs to a decommissioned device, otherwise allowProvisioning=true. The customer's Lambda forwards this verdict to AWS IoT, which aborts the JITP flow on a deny.")
            .Accepts<FleetProvisioningVerifyRequest>("application/json")
            .Produces<FleetProvisioningVerifyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/registered", RegisterAsync)
            .WithName("FleetProvisioningRegister")
            .WithSummary("Post-provisioning hook called once AWS IoT has created the Thing and the operational certificate.")
            .WithDescription("Atomically materialises the cloud-agnostic Device aggregate and the AwsThingBinding (already in Active status, ProvisionedViaJitp=true). Idempotent on the device serial number — a retried JITP flow returns the existing DeviceId without duplicate writes.")
            .Accepts<FleetProvisioningRegisterRequest>("application/json")
            .Produces<FleetProvisioningRegisterResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return group;
    }

    internal static async Task<Results<Ok<FleetProvisioningVerifyResponse>, ProblemHttpResult>> VerifyAsync(
        [FromBody] FleetProvisioningVerifyRequest request,
        [FromServices] IFleetProvisioningService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            return TypedResults.Problem(
                detail: "SerialNumber is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        FleetProvisioningVerifyResponse response = await service
            .VerifyAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return TypedResults.Ok(response);
    }

    internal static async Task<Results<Ok<FleetProvisioningRegisterResponse>, ProblemHttpResult>> RegisterAsync(
        [FromBody] FleetProvisioningRegisterRequest request,
        [FromServices] IFleetProvisioningService service,
        CancellationToken cancellationToken)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.SerialNumber)
            || string.IsNullOrWhiteSpace(request.ThingName)
            || string.IsNullOrWhiteSpace(request.ThingArn)
            || string.IsNullOrWhiteSpace(request.CertificateArn)
            || string.IsNullOrWhiteSpace(request.CertificateSecretArn)
            || string.IsNullOrWhiteSpace(request.Model)
            || string.IsNullOrWhiteSpace(request.FirmwareVersion))
        {
            return TypedResults.Problem(
                detail: "SerialNumber, ThingName, ThingArn, CertificateArn, CertificateSecretArn, Model and FirmwareVersion are all required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            FleetProvisioningRegisterResponse response = await service
                .RegisterAsync(request, cancellationToken)
                .ConfigureAwait(false);
            return TypedResults.Ok(response);
        }
        catch (ArgumentException ex)
        {
            // Domain validation (invalid ThingName format, invalid serial, …)
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }
}
