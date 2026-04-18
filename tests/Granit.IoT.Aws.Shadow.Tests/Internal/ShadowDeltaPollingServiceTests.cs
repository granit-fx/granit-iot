using System.Diagnostics.Metrics;
using Granit.Events;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Shadow.Abstractions;
using Granit.IoT.Aws.Shadow.Diagnostics;
using Granit.IoT.Aws.Shadow.Events;
using Granit.IoT.Aws.Shadow.Internal;
using Granit.IoT.Aws.Shadow.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Aws.Shadow.Tests.Internal;

public sealed class ShadowDeltaPollingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PollOnceAsync_NoBindings_PublishesNothing()
    {
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AwsThingBinding>());
        IDeviceShadowSyncService shadow = Substitute.For<IDeviceShadowSyncService>();
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ShadowDeltaPollingService svc = NewService(reader, shadow, bus);
        await svc.PollOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await bus.DidNotReceiveWithAnyArgs().PublishAsync(Arg.Any<DeviceDesiredStateChangedEvent>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task PollOnceAsync_BindingWithDelta_PublishesEvent()
    {
        AwsThingBinding binding = NewBinding();
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([binding]);

        IDeviceShadowSyncService shadow = Substitute.For<IDeviceShadowSyncService>();
        DeviceShadowSnapshot snap = new(
            Reported: new Dictionary<string, object?>(),
            Desired: new Dictionary<string, object?> { ["k"] = "v" },
            Delta: new Dictionary<string, object?> { ["k"] = "v" },
            Version: 7);
        shadow.GetShadowAsync(binding.ThingName, Arg.Any<CancellationToken>()).Returns(snap);
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ShadowDeltaPollingService svc = NewService(reader, shadow, bus);
        await svc.PollOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await bus.Received(1)
            .PublishAsync(
                Arg.Is<DeviceDesiredStateChangedEvent>(e =>
                    e.DeviceId == binding.DeviceId
                    && e.ShadowVersion == 7
                    && e.Delta.ContainsKey("k")),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task PollOnceAsync_BindingWithEmptyDelta_DoesNotPublish()
    {
        AwsThingBinding binding = NewBinding();
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([binding]);
        IDeviceShadowSyncService shadow = Substitute.For<IDeviceShadowSyncService>();
        DeviceShadowSnapshot snap = new(
            new Dictionary<string, object?>(),
            new Dictionary<string, object?>(),
            new Dictionary<string, object?>(),
            1);
        shadow.GetShadowAsync(binding.ThingName, Arg.Any<CancellationToken>()).Returns(snap);
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ShadowDeltaPollingService svc = NewService(reader, shadow, bus);
        await svc.PollOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await bus.DidNotReceiveWithAnyArgs().PublishAsync(Arg.Any<DeviceDesiredStateChangedEvent>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task PollOnceAsync_ShadowThrows_OneBadBindingDoesNotStopOthers()
    {
        AwsThingBinding bad = NewBinding();
        AwsThingBinding good = NewBinding();
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([bad, good]);

        IDeviceShadowSyncService shadow = Substitute.For<IDeviceShadowSyncService>();
        shadow.GetShadowAsync(bad.ThingName, Arg.Any<CancellationToken>())
            .Returns<DeviceShadowSnapshot?>(_ => throw new InvalidOperationException("boom"));
        DeviceShadowSnapshot okSnap = new(
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["k"] = "v" },
            new Dictionary<string, object?> { ["k"] = "v" },
            1);
        shadow.GetShadowAsync(good.ThingName, Arg.Any<CancellationToken>()).Returns(okSnap);
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ShadowDeltaPollingService svc = NewService(reader, shadow, bus);
        await svc.PollOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await bus.Received(1)
            .PublishAsync(Arg.Is<DeviceDesiredStateChangedEvent>(e => e.DeviceId == good.DeviceId), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private static AwsThingBinding NewBinding() =>
        AwsThingBinding.Create(Guid.NewGuid(), tenantId: null,
            ThingName.Create($"t{Guid.NewGuid():N}-sn"));

    private static ShadowDeltaPollingService NewService(
        IAwsThingBindingReader reader,
        IDeviceShadowSyncService shadow,
        ILocalEventBus bus)
    {
        ServiceCollection services = new();
        services.AddSingleton(reader);
        services.AddSingleton(shadow);
        services.AddSingleton(bus);
        ServiceProvider provider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        AwsShadowOptions opts = new();
        IOptions<AwsShadowOptions> wrapper = Microsoft.Extensions.Options.Options.Create(opts);

        return new ShadowDeltaPollingService(
            scopeFactory,
            wrapper,
            new IoTAwsShadowMetrics(new TestMeterFactory()),
            NullLogger<ShadowDeltaPollingService>.Instance,
            new FakeTimeProvider(Now));
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
