using System.Text.RegularExpressions;
using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.FleetProvisioning.Abstractions;
using Granit.IoT.Aws.FleetProvisioning.Contracts;
using Granit.IoT.Aws.FleetProvisioning.Diagnostics;
using Granit.IoT.Domain;
using Granit.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.FleetProvisioning.Internal;

internal sealed partial class FleetProvisioningService(
    IDeviceReader deviceReader,
    IDeviceWriter deviceWriter,
    IAwsThingBindingReader bindingReader,
    IAwsThingBindingWriter bindingWriter,
    IGuidGenerator guidGenerator,
    ICurrentTenant currentTenant,
    IFleetProvisioningSerialPolicy serialPolicy,
    IoTAwsFleetProvisioningMetrics metrics,
    ILogger<FleetProvisioningService> logger)
    : IFleetProvisioningService
{
    private readonly IDeviceReader _deviceReader = deviceReader;
    private readonly IDeviceWriter _deviceWriter = deviceWriter;
    private readonly IAwsThingBindingReader _bindingReader = bindingReader;
    private readonly IAwsThingBindingWriter _bindingWriter = bindingWriter;
    private readonly IGuidGenerator _guidGenerator = guidGenerator;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IFleetProvisioningSerialPolicy _serialPolicy = serialPolicy;
    private readonly IoTAwsFleetProvisioningMetrics _metrics = metrics;
    private readonly ILogger<FleetProvisioningService> _logger = logger;

    // ARN shapes we persist without change. Validating here means
    // Decommission's ExtractCertificateId can't yield garbage downstream
    // and the tenant's AWS account / region constraint is enforceable.
    [GeneratedRegex(
        @"^arn:aws:iot:[a-z0-9\-]+:\d{12}:thing/[A-Za-z0-9_\-:]+$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250)]
    private static partial Regex ThingArnPattern();

    [GeneratedRegex(
        @"^arn:aws:iot:[a-z0-9\-]+:\d{12}:cert/[a-f0-9]{64}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250)]
    private static partial Regex CertificateArnPattern();

    [GeneratedRegex(
        @"^arn:aws:secretsmanager:[a-z0-9\-]+:\d{12}:secret:[A-Za-z0-9/_+=.@\-]+$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250)]
    private static partial Regex SecretArnPattern();

    public async Task<FleetProvisioningVerifyResponse> VerifyAsync(
        FleetProvisioningVerifyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Tenant is always taken from the authenticated principal, never
        // the body. A body-supplied value that doesn't match is a
        // privilege-escalation attempt and must fail closed.
        Guid? tenantId = ResolveTenant(request.TenantId);

        // Optional per-adopter policy: vendor serial shape, Luhn check,
        // signed bootloader evidence, registry lookup. Default is allow-all.
        SerialPolicyDecision policy = await _serialPolicy
            .EvaluateAsync(request.SerialNumber, tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!policy.Allowed)
        {
            _metrics.RecordVerifyDenied(tenantId);
            FleetProvisioningLog.VerifyDeniedSerialPolicy(_logger, request.SerialNumber);
            return new FleetProvisioningVerifyResponse(
                AllowProvisioning: false,
                Reason: policy.DenyReason ?? "Serial number is not eligible for JITP in this tenant.");
        }

        Device? existing = await _deviceReader
            .FindBySerialNumberAsync(request.SerialNumber, cancellationToken).ConfigureAwait(false);

        if (existing?.Status is DeviceStatus.Decommissioned)
        {
            _metrics.RecordVerifyDenied(tenantId);
            FleetProvisioningLog.VerifyDeniedDecommissioned(_logger, request.SerialNumber);
            return new FleetProvisioningVerifyResponse(
                AllowProvisioning: false,
                Reason: "Device is decommissioned and cannot be re-provisioned through JITP.");
        }

        // If a matching device already exists in another tenant, refuse JITP —
        // this blocks a cross-tenant takeover via serial collision.
        if (existing is not null && existing.TenantId != tenantId)
        {
            _metrics.RecordVerifyDenied(tenantId);
            FleetProvisioningLog.VerifyDeniedTenantMismatch(_logger, request.SerialNumber);
            return new FleetProvisioningVerifyResponse(
                AllowProvisioning: false,
                Reason: "Device serial is bound to a different tenant; re-provisioning denied.");
        }

        _metrics.RecordVerifyAllowed(tenantId);
        return new FleetProvisioningVerifyResponse(AllowProvisioning: true, Reason: null);
    }

    public async Task<FleetProvisioningRegisterResponse> RegisterAsync(
        FleetProvisioningRegisterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Guid? tenantId = ResolveTenant(request.TenantId);
        ValidateArns(request);

        // Idempotency fast-path: if the Device already exists for this serial,
        // we either return its id (binding present) or just create the binding
        // (Device created out-of-band by some other path).
        Device? existing = await _deviceReader
            .FindBySerialNumberAsync(request.SerialNumber, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.TenantId != tenantId)
            {
                // A mismatch here would silently re-bind the victim tenant's
                // device to the attacker's AWS resources — fail closed.
                throw new ArgumentException(
                    "Device serial is bound to a different tenant; JITP register refused.",
                    nameof(request));
            }

            AwsThingBinding? existingBinding = await _bindingReader
                .FindByDeviceAsync(existing.Id, cancellationToken).ConfigureAwait(false);

            if (existingBinding is not null)
            {
                _metrics.RecordRegisterIdempotent(tenantId);
                FleetProvisioningLog.RegisterIdempotent(_logger, request.SerialNumber, existing.Id);
                return new FleetProvisioningRegisterResponse(existing.Id, AlreadyProvisioned: true);
            }

            await CreateJitpBindingAsync(existing.Id, tenantId, request, cancellationToken).ConfigureAwait(false);
            _metrics.RecordRegisterCompleted(tenantId);
            return new FleetProvisioningRegisterResponse(existing.Id, AlreadyProvisioned: false);
        }

        Device device = await CreateAndActivateDeviceAsync(tenantId, request, cancellationToken).ConfigureAwait(false);
        await CreateJitpBindingAsync(device.Id, tenantId, request, cancellationToken).ConfigureAwait(false);

        _metrics.RecordRegisterCompleted(tenantId);
        FleetProvisioningLog.RegisterCompleted(_logger, request.SerialNumber, device.Id);
        return new FleetProvisioningRegisterResponse(device.Id, AlreadyProvisioned: false);
    }

    /// <summary>
    /// Returns the tenant id taken from the authenticated principal
    /// (<see cref="ICurrentTenant.Id"/>). The optional <paramref name="requestTenantId"/>
    /// carried in the body is only accepted when it matches the principal's tenant —
    /// any mismatch is treated as a privilege escalation attempt and fails closed.
    /// </summary>
    private Guid? ResolveTenant(Guid? requestTenantId)
    {
        Guid? principalTenant = _currentTenant.Id;

        if (requestTenantId is { } body && body != principalTenant)
        {
            throw new ArgumentException(
                "Request TenantId does not match the authenticated principal. " +
                "Drop the field or use the principal's tenant only.",
                nameof(requestTenantId));
        }

        return principalTenant;
    }

    private static void ValidateArns(FleetProvisioningRegisterRequest request)
    {
        if (!ThingArnPattern().IsMatch(request.ThingArn))
        {
            throw new ArgumentException(
                "ThingArn is not a well-formed AWS IoT Thing ARN (arn:aws:iot:REGION:ACCOUNT:thing/NAME).",
                nameof(request));
        }

        if (!CertificateArnPattern().IsMatch(request.CertificateArn))
        {
            throw new ArgumentException(
                "CertificateArn is not a well-formed AWS IoT certificate ARN " +
                "(arn:aws:iot:REGION:ACCOUNT:cert/<64-hex-id>).",
                nameof(request));
        }

        if (!SecretArnPattern().IsMatch(request.CertificateSecretArn))
        {
            throw new ArgumentException(
                "CertificateSecretArn is not a well-formed AWS Secrets Manager ARN " +
                "(arn:aws:secretsmanager:REGION:ACCOUNT:secret:NAME).",
                nameof(request));
        }
    }

    private async Task<Device> CreateAndActivateDeviceAsync(
        Guid? tenantId,
        FleetProvisioningRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var device = Device.Create(
            id: _guidGenerator.Create(),
            tenantId: tenantId,
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
        Guid? tenantId,
        FleetProvisioningRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var binding = AwsThingBinding.CreateForJitp(
            deviceId,
            tenantId,
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
