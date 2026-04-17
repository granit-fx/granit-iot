using System.Diagnostics.Metrics;
using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Provisioning.Abstractions;
using Granit.IoT.Aws.Provisioning.Diagnostics;
using Granit.IoT.Aws.Provisioning.Handlers;
using Granit.IoT.Domain;
using Granit.IoT.Events;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Aws.Provisioning.Tests.Handlers;

public sealed class AwsThingBridgeHandlerTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string Serial = "SN-001";

    private readonly IAwsThingBindingReader _bindings = Substitute.For<IAwsThingBindingReader>();
    private readonly IAwsThingBindingWriter _writer = Substitute.For<IAwsThingBindingWriter>();
    private readonly IDeviceReader _devices = Substitute.For<IDeviceReader>();
    private readonly IThingProvisioningService _provisioning = Substitute.For<IThingProvisioningService>();
    private readonly IGuidGenerator _guidGenerator = Substitute.For<IGuidGenerator>();
    private readonly AwsProvisioningMetrics _metrics;

    public AwsThingBridgeHandlerTests()
    {
        IMeterFactory meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(ci => new Meter(ci.Arg<MeterOptions>()));
        _metrics = new AwsProvisioningMetrics(meterFactory);
        _guidGenerator.Create().Returns(_ => Guid.NewGuid());
    }

    [Fact]
    public async Task HandleProvisioned_ReservesBindingAndWalksSaga()
    {
        var deviceId = Guid.NewGuid();
        var device = Device.Create(
            deviceId,
            Tenant,
            DeviceSerialNumber.Create(Serial),
            HardwareModel.Create("Sensor-V1"),
            FirmwareVersion.Create("1.0.0"));
        _devices.FindAsync(deviceId, Arg.Any<CancellationToken>()).Returns(device);
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>())
            .Returns((AwsThingBinding?)null);

        // Track captured binding so we can assert saga calls happened on the same instance.
        AwsThingBinding? captured = null;
        await _writer.AddAsync(
            Arg.Do<AwsThingBinding>(b => captured = b),
            Arg.Any<CancellationToken>());

        var msg = new DeviceProvisionedEvent(deviceId, Serial, Tenant);

        await AwsThingBridgeHandler.HandleAsync(
            msg,
            _bindings,
            _writer,
            _devices,
            _provisioning,
            _guidGenerator,
            _metrics,
            NullLogger<AwsThingBridgeHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.DeviceId.ShouldBe(deviceId);
        captured.ThingName.GetSerialNumber().ShouldBe(Serial);
        await _provisioning.Received(1).EnsureThingAsync(captured, Arg.Any<CancellationToken>());
        await _provisioning.Received(1).EnsureCertificateAndSecretAsync(captured, Arg.Any<CancellationToken>());
        await _provisioning.Received(1).EnsureActivationAsync(captured, Arg.Any<CancellationToken>());
        await _writer.Received().UpdateAsync(captured, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleProvisioned_ReusesExistingBinding_OnReplay()
    {
        var deviceId = Guid.NewGuid();
        var existing = AwsThingBinding.Create(
            deviceId, Tenant, ThingName.From(Tenant, Serial));
        existing.Id = Guid.NewGuid();
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(existing);

        var msg = new DeviceProvisionedEvent(deviceId, Serial, Tenant);

        await AwsThingBridgeHandler.HandleAsync(
            msg,
            _bindings,
            _writer,
            _devices,
            _provisioning,
            _guidGenerator,
            _metrics,
            NullLogger<AwsThingBridgeHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);

        await _writer.DidNotReceive().AddAsync(Arg.Any<AwsThingBinding>(), Arg.Any<CancellationToken>());
        await _provisioning.Received().EnsureThingAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleProvisioned_BypassesSaga_ForJitpBinding()
    {
        var deviceId = Guid.NewGuid();
        var jitp = AwsThingBinding.CreateForJitp(
            deviceId,
            Tenant,
            ThingName.From(Tenant, Serial),
            "arn:aws:iot:eu-west-1:123:thing/sample",
            "arn:aws:iot:eu-west-1:123:cert/abc",
            "arn:aws:secretsmanager:eu-west-1:123:secret:x");
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(jitp);

        var msg = new DeviceProvisionedEvent(deviceId, Serial, Tenant);

        await AwsThingBridgeHandler.HandleAsync(
            msg,
            _bindings,
            _writer,
            _devices,
            _provisioning,
            _guidGenerator,
            _metrics,
            NullLogger<AwsThingBridgeHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);

        await _provisioning.DidNotReceive().EnsureThingAsync(Arg.Any<AwsThingBinding>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleProvisioned_NoOps_WhenDeviceWasDeletedBeforeDispatch()
    {
        var deviceId = Guid.NewGuid();
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>())
            .Returns((AwsThingBinding?)null);
        _devices.FindAsync(deviceId, Arg.Any<CancellationToken>()).Returns((Device?)null);

        var msg = new DeviceProvisionedEvent(deviceId, Serial, Tenant);

        await AwsThingBridgeHandler.HandleAsync(
            msg,
            _bindings,
            _writer,
            _devices,
            _provisioning,
            _guidGenerator,
            _metrics,
            NullLogger<AwsThingBridgeHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);

        await _writer.DidNotReceive().AddAsync(Arg.Any<AwsThingBinding>(), Arg.Any<CancellationToken>());
        await _provisioning.DidNotReceive().EnsureThingAsync(Arg.Any<AwsThingBinding>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleDecommissioned_DelegatesToProvisioningServiceAndDeletesBinding()
    {
        var deviceId = Guid.NewGuid();
        var binding = AwsThingBinding.Create(
            deviceId, Tenant, ThingName.From(Tenant, Serial));
        binding.Id = Guid.NewGuid();
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(binding);

        await AwsThingBridgeHandler.HandleAsync(
            new DeviceDecommissionedEvent(deviceId, Tenant),
            _bindings,
            _writer,
            _provisioning,
            TestContext.Current.CancellationToken);

        await _provisioning.Received(1).DecommissionAsync(binding, Arg.Any<CancellationToken>());
        await _writer.Received(1).DeleteAsync(binding, Arg.Any<CancellationToken>());
    }
}
