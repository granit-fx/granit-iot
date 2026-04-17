using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Jobs.Abstractions;
using Granit.IoT.Aws.Jobs.Handlers;
using Granit.IoT.Aws.Shadow.Events;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Aws.Jobs.Tests.Handlers;

public sealed class ShadowDesiredStateCommandHandlerTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string Serial = "SN-001";

    private readonly IAwsThingBindingReader _bindings = Substitute.For<IAwsThingBindingReader>();
    private readonly IDeviceCommandDispatcher _dispatcher = Substitute.For<IDeviceCommandDispatcher>();

    [Fact]
    public async Task DispatchesJob_OnNonEmptyDelta()
    {
        var deviceId = Guid.NewGuid();
        AwsThingBinding binding = ActiveBinding(deviceId);
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(binding);
        _dispatcher.DispatchAsync(
                Arg.Any<IDeviceCommand>(),
                Arg.Any<DeviceCommandTarget>(),
                Arg.Any<CancellationToken>())
            .Returns("granit-some-job-id");

        var evt = new DeviceDesiredStateChangedEvent(
            deviceId,
            binding.ThingName.Value,
            new Dictionary<string, object?> { ["status"] = "Suspended" },
            ShadowVersion: 17,
            Tenant);

        await ShadowDesiredStateCommandHandler.HandleAsync(
            evt,
            _bindings,
            _dispatcher,
            NullLogger<ShadowDesiredStateCommandHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);

        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<IDeviceCommand>(c =>
                c.Operation == ShadowDesiredStateCommandHandler.OperationName
                && c.Parameters.Count == 1
                && (string)c.Parameters["status"]! == "Suspended"),
            Arg.Is<DeviceCommandTarget>(t =>
                t.Mode == DeviceCommandTargetMode.Thing
                && t.Value == binding.ThingArn),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DerivesDeterministicCorrelationId_FromDeviceAndShadowVersion()
    {
        var deviceId = Guid.NewGuid();
        AwsThingBinding binding = ActiveBinding(deviceId);
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(binding);

        var captured = new List<Guid>();
        _dispatcher.DispatchAsync(
                Arg.Do<IDeviceCommand>(c => captured.Add(c.CorrelationId)),
                Arg.Any<DeviceCommandTarget>(),
                Arg.Any<CancellationToken>())
            .Returns("granit-job");

        var evt = new DeviceDesiredStateChangedEvent(
            deviceId,
            binding.ThingName.Value,
            new Dictionary<string, object?> { ["status"] = "Active" },
            ShadowVersion: 42,
            Tenant);

        await ShadowDesiredStateCommandHandler.HandleAsync(
            evt, _bindings, _dispatcher,
            NullLogger<ShadowDesiredStateCommandHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);
        await ShadowDesiredStateCommandHandler.HandleAsync(
            evt, _bindings, _dispatcher,
            NullLogger<ShadowDesiredStateCommandHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);

        captured.Count.ShouldBe(2);
        captured[0].ShouldBe(captured[1]);
    }

    [Fact]
    public async Task NoOp_OnEmptyDelta()
    {
        var evt = new DeviceDesiredStateChangedEvent(
            Guid.NewGuid(), "x", new Dictionary<string, object?>(), 1, Tenant);

        await ShadowDesiredStateCommandHandler.HandleAsync(
            evt, _bindings, _dispatcher,
            NullLogger<ShadowDesiredStateCommandHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(
            Arg.Any<IDeviceCommand>(),
            Arg.Any<DeviceCommandTarget>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoOp_WhenBindingNotActive()
    {
        var deviceId = Guid.NewGuid();
        var pending = AwsThingBinding.Create(deviceId, Tenant, ThingName.From(Tenant, Serial));
        pending.Id = Guid.NewGuid();
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(pending);

        var evt = new DeviceDesiredStateChangedEvent(
            deviceId, "x", new Dictionary<string, object?> { ["status"] = "Active" }, 1, Tenant);

        await ShadowDesiredStateCommandHandler.HandleAsync(
            evt, _bindings, _dispatcher,
            NullLogger<ShadowDesiredStateCommandHandlerCategory>.Instance,
            TestContext.Current.CancellationToken);

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(
            Arg.Any<IDeviceCommand>(),
            Arg.Any<DeviceCommandTarget>(),
            Arg.Any<CancellationToken>());
    }

    private static AwsThingBinding ActiveBinding(Guid deviceId)
    {
        var binding = AwsThingBinding.CreateForJitp(
            deviceId,
            Tenant,
            ThingName.From(Tenant, Serial),
            "arn:aws:iot:eu-west-1:123:thing/sample",
            "arn:aws:iot:eu-west-1:123:cert/abc",
            "arn:aws:secretsmanager:eu-west-1:123:secret:x");
        binding.Id = Guid.NewGuid();
        return binding;
    }
}
