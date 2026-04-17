using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.FleetProvisioning.Abstractions;
using Granit.IoT.Aws.FleetProvisioning.Contracts;
using Granit.IoT.Aws.FleetProvisioning.Diagnostics;
using Granit.IoT.Domain;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.FleetProvisioning.Internal;

internal sealed class FleetProvisioningService(
    IDeviceReader deviceReader,
    IDeviceWriter deviceWriter,
    IAwsThingBindingReader bindingReader,
    IAwsThingBindingWriter bindingWriter,
    IGuidGenerator guidGenerator,
    FleetProvisioningMetrics metrics,
    ILogger<FleetProvisioningService> logger)
    : IFleetProvisioningService
{
    private readonly IDeviceReader _deviceReader = deviceReader;
    private readonly IDeviceWriter _deviceWriter = deviceWriter;
    private readonly IAwsThingBindingReader _bindingReader = bindingReader;
    private readonly IAwsThingBindingWriter _bindingWriter = bindingWriter;
    private readonly IGuidGenerator _guidGenerator = guidGenerator;
    private readonly FleetProvisioningMetrics _metrics = metrics;
    private readonly ILogger<FleetProvisioningService> _logger = logger;

    public async Task<FleetProvisioningVerifyResponse> VerifyAsync(
        FleetProvisioningVerifyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Device? existing = await _deviceReader
            .FindBySerialNumberAsync(request.SerialNumber, cancellationToken).ConfigureAwait(false);

        if (existing?.Status is DeviceStatus.Decommissioned)
        {
            _metrics.RecordVerifyDenied(request.TenantId);
            FleetProvisioningLog.VerifyDeniedDecommissioned(_logger, request.SerialNumber);
            return new FleetProvisioningVerifyResponse(
                AllowProvisioning: false,
                Reason: "Device is decommissioned and cannot be re-provisioned through JITP.");
        }

        _metrics.RecordVerifyAllowed(request.TenantId);
        return new FleetProvisioningVerifyResponse(AllowProvisioning: true, Reason: null);
    }

    public async Task<FleetProvisioningRegisterResponse> RegisterAsync(
        FleetProvisioningRegisterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Idempotency fast-path: if the Device already exists for this serial,
        // we either return its id (binding present) or just create the binding
        // (Device created out-of-band by some other path).
        Device? existing = await _deviceReader
            .FindBySerialNumberAsync(request.SerialNumber, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            AwsThingBinding? existingBinding = await _bindingReader
                .FindByDeviceAsync(existing.Id, cancellationToken).ConfigureAwait(false);

            if (existingBinding is not null)
            {
                _metrics.RecordRegisterIdempotent(request.TenantId);
                FleetProvisioningLog.RegisterIdempotent(_logger, request.SerialNumber, existing.Id);
                return new FleetProvisioningRegisterResponse(existing.Id, AlreadyProvisioned: true);
            }

            await CreateJitpBindingAsync(existing.Id, request, cancellationToken).ConfigureAwait(false);
            _metrics.RecordRegisterCompleted(request.TenantId);
            return new FleetProvisioningRegisterResponse(existing.Id, AlreadyProvisioned: false);
        }

        Device device = await CreateAndActivateDeviceAsync(request, cancellationToken).ConfigureAwait(false);
        await CreateJitpBindingAsync(device.Id, request, cancellationToken).ConfigureAwait(false);

        _metrics.RecordRegisterCompleted(request.TenantId);
        FleetProvisioningLog.RegisterCompleted(_logger, request.SerialNumber, device.Id);
        return new FleetProvisioningRegisterResponse(device.Id, AlreadyProvisioned: false);
    }

    private async Task<Device> CreateAndActivateDeviceAsync(
        FleetProvisioningRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var device = Device.Create(
            id: _guidGenerator.Create(),
            tenantId: request.TenantId,
            serialNumber: DeviceSerialNumber.Create(request.SerialNumber),
            model: HardwareModel.Create(request.Model),
            firmware: FirmwareVersion.Create(request.FirmwareVersion),
            label: request.Label);

        // The JITP path activates the device immediately — it is already
        // operating against AWS IoT by the time we hear about it.
        device.Activate();
        await _deviceWriter.AddAsync(device, cancellationToken).ConfigureAwait(false);
        return device;
    }

    private Task CreateJitpBindingAsync(
        Guid deviceId,
        FleetProvisioningRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var binding = AwsThingBinding.CreateForJitp(
            deviceId,
            request.TenantId,
            ThingName.Create(request.ThingName),
            request.ThingArn,
            request.CertificateArn,
            request.CertificateSecretArn);
        binding.Id = _guidGenerator.Create();

        if (request.ClaimCertificateExpiresAt is { } expires)
        {
            binding.RecordClaimCertificateExpiry(expires);
        }

        return _bindingWriter.AddAsync(binding, cancellationToken);
    }
}
