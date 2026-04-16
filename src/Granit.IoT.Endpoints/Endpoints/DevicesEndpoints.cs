using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Domain;
using Granit.IoT.Endpoints.Dtos;
using Granit.IoT.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Granit.IoT.Endpoints.Endpoints;

internal static class DevicesEndpoints
{
    internal static RouteGroupBuilder MapDeviceRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListDevicesAsync)
            .WithName("ListDevices")
            .WithSummary("Lists all devices for the current tenant.")
            .WithDescription("Returns a paginated list of devices, optionally filtered by status.")
            .Produces<IReadOnlyList<DeviceResponse>>()
            .RequireAuthorization(IoTPermissions.Devices.Read);

        group.MapGet("/{id:guid}", GetDeviceByIdAsync)
            .WithName("GetDeviceById")
            .WithSummary("Gets a device by ID.")
            .WithDescription("Returns the device details or 404 if not found in the current tenant.")
            .Produces<DeviceResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(IoTPermissions.Devices.Read);

        group.MapPost("/", ProvisionDeviceAsync)
            .WithName("ProvisionDevice")
            .WithSummary("Provisions a new IoT device.")
            .WithDescription("Creates a new device in Provisioning status. Returns 409 if the serial number already exists.")
            .Produces<DeviceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .RequireAuthorization(IoTPermissions.Devices.Manage);

        group.MapPut("/{id:guid}", UpdateDeviceAsync)
            .WithName("UpdateDevice")
            .WithSummary("Updates a device.")
            .WithDescription("Updates firmware version or label of an existing device.")
            .Produces<DeviceResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(IoTPermissions.Devices.Manage);

        group.MapDelete("/{id:guid}", DecommissionDeviceAsync)
            .WithName("DecommissionDevice")
            .WithSummary("Decommissions a device.")
            .WithDescription("Soft-deletes a device. The device must be in Suspended or Provisioning status — active devices must be suspended first.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(IoTPermissions.Devices.Manage);

        return group;
    }

    private static async Task<Ok<IReadOnlyList<DeviceResponse>>> ListDevicesAsync(
        [FromServices] IDeviceReader reader,
        DeviceStatus? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Device> devices = await reader
            .ListAsync(status, page, pageSize, cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok<IReadOnlyList<DeviceResponse>>(
            devices.Select(ToResponse).ToList());
    }

    private static async Task<Results<Ok<DeviceResponse>, NotFound>> GetDeviceByIdAsync(
        Guid id,
        [FromServices] IDeviceReader reader,
        CancellationToken cancellationToken = default)
    {
        Device? device = await reader.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (device is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToResponse(device));
    }

    private static async Task<Results<Created<DeviceResponse>, ProblemHttpResult>> ProvisionDeviceAsync(
        DeviceProvisionRequest request,
        [FromServices] IDeviceReader reader,
        [FromServices] IDeviceWriter writer,
        [FromServices] IGuidGenerator guidGenerator,
        CancellationToken cancellationToken = default)
    {
        // Check for existing serial number to avoid opaque DB constraint violation
        if (await reader.ExistsAsync(request.SerialNumber, cancellationToken).ConfigureAwait(false))
        {
            return TypedResults.Problem(
                detail: $"A device with serial number '{request.SerialNumber}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var device = Device.Create(
            guidGenerator.Create(),
            tenantId: null, // Set by multi-tenant interceptor
            DeviceSerialNumber.Create(request.SerialNumber),
            HardwareModel.Create(request.HardwareModel),
            FirmwareVersion.Create(request.FirmwareVersion),
            request.Label);

        await writer.AddAsync(device, cancellationToken).ConfigureAwait(false);

        DeviceResponse response = ToResponse(device);
        return TypedResults.Created($"/iot/devices/{device.Id}", response);
    }

    private static async Task<Results<Ok<DeviceResponse>, NotFound>> UpdateDeviceAsync(
        Guid id,
        DeviceUpdateRequest request,
        [FromServices] IDeviceReader reader,
        [FromServices] IDeviceWriter writer,
        CancellationToken cancellationToken = default)
    {
        Device? device = await reader.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (device is null)
        {
            return TypedResults.NotFound();
        }

        if (request.FirmwareVersion is not null)
        {
            device.UpdateFirmware(FirmwareVersion.Create(request.FirmwareVersion));
        }

        if (request.Label is not null)
        {
            device.UpdateLabel(request.Label);
        }

        await writer.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(ToResponse(device));
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DecommissionDeviceAsync(
        Guid id,
        [FromServices] IDeviceReader reader,
        [FromServices] IDeviceWriter writer,
        CancellationToken cancellationToken = default)
    {
        Device? device = await reader.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (device is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            device.Decommission();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }

        await writer.DeleteAsync(device, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static DeviceResponse ToResponse(Device device) => new(
        device.Id,
        device.SerialNumber,
        device.Model,
        device.Firmware,
        device.Status.ToString(),
        device.Label,
        device.LastHeartbeatAt,
        device.CreatedAt,
        device.ModifiedAt);
}
