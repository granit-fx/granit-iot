using System.Diagnostics.Metrics;
using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.FleetProvisioning.Contracts;
using Granit.IoT.Aws.FleetProvisioning.Diagnostics;
using Granit.IoT.Aws.FleetProvisioning.Internal;
using Granit.IoT.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Aws.FleetProvisioning.Tests.Internal;

public sealed class FleetProvisioningServiceTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly IDeviceReader _deviceReader = Substitute.For<IDeviceReader>();
    private readonly IDeviceWriter _deviceWriter = Substitute.For<IDeviceWriter>();
    private readonly IAwsThingBindingReader _bindingReader = Substitute.For<IAwsThingBindingReader>();
    private readonly IAwsThingBindingWriter _bindingWriter = Substitute.For<IAwsThingBindingWriter>();
    private readonly IGuidGenerator _guidGenerator = Substitute.For<IGuidGenerator>();
    private readonly FleetProvisioningMetrics _metrics = new(new TestMeterFactory());

    public FleetProvisioningServiceTests()
    {
        _guidGenerator.Create().Returns(_ => Guid.NewGuid());
    }

    [Fact]
    public async Task VerifyAsync_AllowsUnknownSerial()
    {
        FleetProvisioningService service = NewService();
        _deviceReader.FindBySerialNumberAsync("SN-001", Arg.Any<CancellationToken>())
            .Returns((Device?)null);

        FleetProvisioningVerifyResponse response = await service.VerifyAsync(
            new FleetProvisioningVerifyRequest("SN-001", Tenant), TestContext.Current.CancellationToken);

        response.AllowProvisioning.ShouldBeTrue();
        response.Reason.ShouldBeNull();
    }

    [Fact]
    public async Task VerifyAsync_DeniesDecommissionedDevice()
    {
        FleetProvisioningService service = NewService();
        Device decommissioned = NewDevice("SN-001");
        decommissioned.Decommission();
        _deviceReader.FindBySerialNumberAsync("SN-001", Arg.Any<CancellationToken>())
            .Returns(decommissioned);

        FleetProvisioningVerifyResponse response = await service.VerifyAsync(
            new FleetProvisioningVerifyRequest("SN-001", Tenant), TestContext.Current.CancellationToken);

        response.AllowProvisioning.ShouldBeFalse();
        response.Reason.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_CreatesDeviceAndBinding_OnFirstCall()
    {
        FleetProvisioningService service = NewService();
        _deviceReader.FindBySerialNumberAsync("SN-001", Arg.Any<CancellationToken>())
            .Returns((Device?)null);

        FleetProvisioningRegisterResponse response = await service.RegisterAsync(
            ValidRegisterRequest(), TestContext.Current.CancellationToken);

        response.AlreadyProvisioned.ShouldBeFalse();
        response.DeviceId.ShouldNotBe(Guid.Empty);
        await _deviceWriter.Received(1).AddAsync(
            Arg.Is<Device>(d => d.Status == DeviceStatus.Active),
            Arg.Any<CancellationToken>());
        await _bindingWriter.Received(1).AddAsync(
            Arg.Is<AwsThingBinding>(b =>
                b.ProvisionedViaJitp
                && b.ProvisioningStatus == AwsThingProvisioningStatus.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_IsIdempotent_WhenDeviceAndBindingAlreadyExist()
    {
        FleetProvisioningService service = NewService();
        Device existingDevice = NewDevice("SN-001");
        var existingBinding = AwsThingBinding.CreateForJitp(
            existingDevice.Id, Tenant, ThingName.From(Tenant, "SN-001"),
            "arn:thing", "arn:cert", "arn:secret");
        existingBinding.Id = Guid.NewGuid();

        _deviceReader.FindBySerialNumberAsync("SN-001", Arg.Any<CancellationToken>())
            .Returns(existingDevice);
        _bindingReader.FindByDeviceAsync(existingDevice.Id, Arg.Any<CancellationToken>())
            .Returns(existingBinding);

        FleetProvisioningRegisterResponse response = await service.RegisterAsync(
            ValidRegisterRequest(), TestContext.Current.CancellationToken);

        response.AlreadyProvisioned.ShouldBeTrue();
        response.DeviceId.ShouldBe(existingDevice.Id);
        await _deviceWriter.DidNotReceive().AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>());
        await _bindingWriter.DidNotReceive().AddAsync(Arg.Any<AwsThingBinding>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_CreatesBinding_WhenDeviceExistsButBindingMissing()
    {
        FleetProvisioningService service = NewService();
        Device existingDevice = NewDevice("SN-001");
        _deviceReader.FindBySerialNumberAsync("SN-001", Arg.Any<CancellationToken>())
            .Returns(existingDevice);
        _bindingReader.FindByDeviceAsync(existingDevice.Id, Arg.Any<CancellationToken>())
            .Returns((AwsThingBinding?)null);

        FleetProvisioningRegisterResponse response = await service.RegisterAsync(
            ValidRegisterRequest(), TestContext.Current.CancellationToken);

        response.AlreadyProvisioned.ShouldBeFalse();
        response.DeviceId.ShouldBe(existingDevice.Id);
        await _deviceWriter.DidNotReceive().AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>());
        await _bindingWriter.Received(1).AddAsync(Arg.Any<AwsThingBinding>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_RecordsClaimCertificateExpiry_WhenProvided()
    {
        FleetProvisioningService service = NewService();
        _deviceReader.FindBySerialNumberAsync("SN-001", Arg.Any<CancellationToken>())
            .Returns((Device?)null);

        DateTimeOffset expiry = DateTimeOffset.UtcNow.AddDays(45);
        FleetProvisioningRegisterRequest request = ValidRegisterRequest() with
        {
            ClaimCertificateExpiresAt = expiry,
        };

        await service.RegisterAsync(request, TestContext.Current.CancellationToken);

        await _bindingWriter.Received(1).AddAsync(
            Arg.Is<AwsThingBinding>(b => b.ClaimCertificateExpiresAt == expiry),
            Arg.Any<CancellationToken>());
    }

    private FleetProvisioningService NewService() =>
        new(_deviceReader, _deviceWriter, _bindingReader, _bindingWriter, _guidGenerator,
            _metrics, NullLogger<FleetProvisioningService>.Instance);

    private static Device NewDevice(string serial) =>
        Device.Create(
            Guid.NewGuid(),
            Tenant,
            DeviceSerialNumber.Create(serial),
            HardwareModel.Create("Sensor-V1"),
            FirmwareVersion.Create("1.0.0"));

    private static FleetProvisioningRegisterRequest ValidRegisterRequest() =>
        new(
            SerialNumber: "SN-001",
            TenantId: Tenant,
            ThingName: ThingName.From(Tenant, "SN-001").Value,
            ThingArn: "arn:aws:iot:eu-west-1:123:thing/sample",
            CertificateArn: "arn:aws:iot:eu-west-1:123:cert/abcdef",
            CertificateSecretArn: "arn:aws:secretsmanager:eu-west-1:123:secret:device",
            Model: "Sensor-V1",
            FirmwareVersion: "1.0.0",
            Label: "Tilted",
            ClaimCertificateExpiresAt: null);

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
