using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Shadow.Abstractions;
using Granit.IoT.Aws.Shadow.Handlers;
using Granit.IoT.Aws.Shadow.Options;
using Granit.IoT.Events;
using Granit.Timing;
using NSubstitute;
using Shouldly;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Granit.IoT.Aws.Shadow.Tests.Handlers;

public sealed class DeviceLifecycleShadowHandlerTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string Serial = "SN-001";

    private readonly IAwsThingBindingReader _bindings = Substitute.For<IAwsThingBindingReader>();
    private readonly IDeviceShadowSyncService _shadow = Substitute.For<IDeviceShadowSyncService>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public DeviceLifecycleShadowHandlerTests()
    {
        _clock.Now.Returns(new DateTimeOffset(2026, 4, 17, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Activated_PushesReportedStatusActive()
    {
        var deviceId = Guid.NewGuid();
        AwsThingBinding active = ActiveBinding(deviceId);
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(active);

        await DeviceLifecycleShadowHandler.HandleAsync(
            new DeviceActivatedEvent(deviceId, Serial, Tenant),
            _bindings,
            _shadow,
            DefaultOptions(),
            _clock,
            TestContext.Current.CancellationToken);

        await _shadow.Received(1).PushReportedAsync(
            active.ThingName,
            Arg.Is<IReadOnlyDictionary<string, object?>>(d => (string)d["status"]! == "Active"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Suspended_PushesReportedStatusSuspended()
    {
        var deviceId = Guid.NewGuid();
        AwsThingBinding active = ActiveBinding(deviceId);
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(active);

        await DeviceLifecycleShadowHandler.HandleAsync(
            new DeviceSuspendedEvent(deviceId, "Maintenance", Tenant),
            _bindings,
            _shadow,
            DefaultOptions(),
            _clock,
            TestContext.Current.CancellationToken);

        await _shadow.Received(1).PushReportedAsync(
            Arg.Any<ThingName>(),
            Arg.Is<IReadOnlyDictionary<string, object?>>(d => (string)d["status"]! == "Suspended"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoOp_WhenAutoPushDisabled()
    {
        var deviceId = Guid.NewGuid();
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(ActiveBinding(deviceId));

        Microsoft.Extensions.Options.IOptions<AwsShadowOptions> disabled =
            MsOptions.Create(new AwsShadowOptions { AutoPushLifecycleStatus = false });

        await DeviceLifecycleShadowHandler.HandleAsync(
            new DeviceActivatedEvent(deviceId, Serial, Tenant),
            _bindings,
            _shadow,
            disabled,
            _clock,
            TestContext.Current.CancellationToken);

        await _shadow.DidNotReceive().PushReportedAsync(
            Arg.Any<ThingName>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoOp_WhenBindingNotYetActive()
    {
        var deviceId = Guid.NewGuid();
        var pending = AwsThingBinding.Create(
            deviceId, Tenant, ThingName.From(Tenant, Serial));
        pending.Id = Guid.NewGuid();
        _bindings.FindByDeviceAsync(deviceId, Arg.Any<CancellationToken>()).Returns(pending);

        await DeviceLifecycleShadowHandler.HandleAsync(
            new DeviceActivatedEvent(deviceId, Serial, Tenant),
            _bindings,
            _shadow,
            DefaultOptions(),
            _clock,
            TestContext.Current.CancellationToken);

        await _shadow.DidNotReceive().PushReportedAsync(
            Arg.Any<ThingName>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    private static Microsoft.Extensions.Options.IOptions<AwsShadowOptions> DefaultOptions() =>
        MsOptions.Create(new AwsShadowOptions { AutoPushLifecycleStatus = true });

    private static AwsThingBinding ActiveBinding(Guid deviceId)
    {
        var binding = AwsThingBinding.CreateForJitp(
            deviceId,
            Tenant,
            ThingName.From(Tenant, Serial),
            "arn:aws:iot:eu-west-1:123:thing/x",
            "arn:aws:iot:eu-west-1:123:cert/y",
            "arn:aws:secretsmanager:eu-west-1:123:secret:z");
        binding.Id = Guid.NewGuid();
        return binding;
    }
}
