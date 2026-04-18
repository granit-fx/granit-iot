using Granit.Events;
using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Diagnostics;
using Granit.IoT.Domain;
using Granit.IoT.Events;
using Granit.IoT.Wolverine.Abstractions;
using Granit.IoT.Wolverine.Handlers;
using Granit.MultiTenancy;
using Granit.Timing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Wolverine;

public sealed class TelemetryIngestedHandlerTests
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid GeneratedId = Guid.NewGuid();
    private static readonly DateTimeOffset RecordedAt = new(2026, 4, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NowClock = new(2026, 4, 16, 12, 0, 5, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_NullMessage_Throws()
    {
        Deps deps = NewDeps();

        await Should.ThrowAsync<ArgumentNullException>(() => TelemetryIngestedHandler.HandleAsync(
            null!,
            deps.TelemetryWriter,
            deps.DeviceWriter,
            deps.Evaluator,
            deps.EventBus,
            deps.GuidGenerator,
            deps.Clock,
            deps.Metrics,
            deps.CurrentTenant,
            NullLogger<TelemetryIngestedHandlerCategory>.Instance,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_UnknownDevice_LogsAndReturnsWithoutSideEffects()
    {
        Deps deps = NewDeps();
        TelemetryIngestedEto msg = NewMessage(deviceId: null);

        await TelemetryIngestedHandler.HandleAsync(
            msg, deps.TelemetryWriter, deps.DeviceWriter, deps.Evaluator, deps.EventBus,
            deps.GuidGenerator, deps.Clock, deps.Metrics, deps.CurrentTenant,
            NullLogger<TelemetryIngestedHandlerCategory>.Instance,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await deps.TelemetryWriter.DidNotReceiveWithAnyArgs()
            .AppendAsync(default!, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await deps.DeviceWriter.DidNotReceiveWithAnyArgs()
            .UpdateHeartbeatAsync(default, default, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await deps.EventBus.DidNotReceiveWithAnyArgs()
            .PublishAsync(Arg.Any<TelemetryThresholdExceededEto>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task HandleAsync_KnownDeviceNoBreaches_PersistsAndUpdatesHeartbeat()
    {
        Deps deps = NewDeps();
        TelemetryIngestedEto msg = NewMessage(deviceId: DeviceId);

        await TelemetryIngestedHandler.HandleAsync(
            msg, deps.TelemetryWriter, deps.DeviceWriter, deps.Evaluator, deps.EventBus,
            deps.GuidGenerator, deps.Clock, deps.Metrics, deps.CurrentTenant,
            NullLogger<TelemetryIngestedHandlerCategory>.Instance,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await deps.TelemetryWriter.Received(1)
            .AppendAsync(
                Arg.Is<TelemetryPoint>(p =>
                    p.Id == GeneratedId
                    && p.DeviceId == DeviceId
                    && p.TenantId == TenantId
                    && p.RecordedAt == RecordedAt
                    && p.MessageId == "msg-1"
                    && p.Source == "test"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await deps.DeviceWriter.Received(1)
            .UpdateHeartbeatAsync(DeviceId, NowClock, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await deps.EventBus.DidNotReceiveWithAnyArgs()
            .PublishAsync(Arg.Any<TelemetryThresholdExceededEto>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        deps.CurrentTenant.Received(1).Change(TenantId);
    }

    [Fact]
    public async Task HandleAsync_BreachesReturned_PublishesEachOne()
    {
        Deps deps = NewDeps();
        TelemetryThresholdExceededEto breach1 = new(DeviceId, TenantId, "temp", 99, 80, RecordedAt);
        TelemetryThresholdExceededEto breach2 = new(DeviceId, TenantId, "humidity", 110, 100, RecordedAt);
        deps.Evaluator
            .EvaluateAsync(DeviceId, TenantId, Arg.Any<IReadOnlyDictionary<string, double>>(), RecordedAt, Arg.Any<CancellationToken>())
            .Returns([breach1, breach2]);

        TelemetryIngestedEto msg = NewMessage(deviceId: DeviceId);

        await TelemetryIngestedHandler.HandleAsync(
            msg, deps.TelemetryWriter, deps.DeviceWriter, deps.Evaluator, deps.EventBus,
            deps.GuidGenerator, deps.Clock, deps.Metrics, deps.CurrentTenant,
            NullLogger<TelemetryIngestedHandlerCategory>.Instance,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await deps.EventBus.Received(1).PublishAsync(breach1, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await deps.EventBus.Received(1).PublishAsync(breach2, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static TelemetryIngestedEto NewMessage(Guid? deviceId) => new(
        MessageId: "msg-1",
        DeviceExternalId: "SN-1",
        DeviceId: deviceId,
        TenantId: TenantId,
        RecordedAt: RecordedAt,
        Metrics: new Dictionary<string, double> { ["temp"] = 22.5 },
        Source: "test",
        Tags: null);

    private static Deps NewDeps()
    {
        ITelemetryWriter telemetryWriter = Substitute.For<ITelemetryWriter>();
        IDeviceWriter deviceWriter = Substitute.For<IDeviceWriter>();
        IDeviceThresholdEvaluator evaluator = Substitute.For<IDeviceThresholdEvaluator>();
        evaluator
            .EvaluateAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<IReadOnlyDictionary<string, double>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        IDistributedEventBus eventBus = Substitute.For<IDistributedEventBus>();
        IGuidGenerator guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(GeneratedId);
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(NowClock);
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());

        return new Deps(
            telemetryWriter,
            deviceWriter,
            evaluator,
            eventBus,
            guidGenerator,
            clock,
            new IoTMetrics(new EmptyMeterFactory()),
            currentTenant);
    }

    private sealed record Deps(
        ITelemetryWriter TelemetryWriter,
        IDeviceWriter DeviceWriter,
        IDeviceThresholdEvaluator Evaluator,
        IDistributedEventBus EventBus,
        IGuidGenerator GuidGenerator,
        IClock Clock,
        IoTMetrics Metrics,
        ICurrentTenant CurrentTenant);
}
