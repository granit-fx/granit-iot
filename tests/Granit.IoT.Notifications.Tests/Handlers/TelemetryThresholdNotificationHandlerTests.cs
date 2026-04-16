#pragma warning disable CA2012 // NSubstitute ValueTask setup — consumed exactly once per call
using System.Diagnostics.Metrics;
using Granit.Domain;
using Granit.IoT.Diagnostics;
using Granit.IoT.Events;
using Granit.IoT.Notifications;
using Granit.IoT.Notifications.Abstractions;
using Granit.IoT.Notifications.Handlers;
using Granit.Notifications;
using Granit.Notifications.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Notifications.Tests.Handlers;

public sealed class TelemetryThresholdNotificationHandlerTests
{
    private static readonly Guid DeviceId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset RecordedAt = new(2026, 4, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_NullMessage_ThrowsArgumentNullException()
    {
        IAlertThrottle throttle = AcceptingThrottle();
        INotificationPublisher publisher = Substitute.For<INotificationPublisher>();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await TelemetryThresholdNotificationHandler.HandleAsync(
                message: null!,
                throttle,
                publisher,
                BuildMetrics(),
                NullLogger<TelemetryThresholdNotificationHandlerCategory>.Instance,
                TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
    }

    [Fact]
    public async Task HandleAsync_ThrottleAcquired_PublishesToDeviceFollowers()
    {
        IAlertThrottle throttle = AcceptingThrottle();
        INotificationPublisher publisher = Substitute.For<INotificationPublisher>();

        TelemetryThresholdExceededEto message = new(DeviceId, TenantId, "temperature", 41.5, 40.0, RecordedAt);

        await TelemetryThresholdNotificationHandler
            .HandleAsync(message, throttle, publisher, BuildMetrics(), NullLogger<TelemetryThresholdNotificationHandlerCategory>.Instance, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await publisher.Received(1)
            .PublishToEntityFollowersAsync(
                Arg.Is<NotificationType<IoTTelemetryThresholdAlertData>>(t => t == IoTTelemetryThresholdAlertNotificationType.Instance),
                Arg.Is<IoTTelemetryThresholdAlertData>(d =>
                    d.DeviceId == DeviceId
                    && d.MetricName == "temperature"
                    && d.ObservedValue == 41.5
                    && d.ThresholdValue == 40.0
                    && d.RecordedAt == RecordedAt),
                Arg.Is<EntityReference>(e =>
                    e.EntityType == "Device"
                    && e.EntityId == DeviceId.ToString("N")),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task HandleAsync_ThrottleRejects_DoesNotPublish()
    {
        IAlertThrottle throttle = Substitute.For<IAlertThrottle>();
        throttle.TryAcquireAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        INotificationPublisher publisher = Substitute.For<INotificationPublisher>();

        TelemetryThresholdExceededEto message = new(DeviceId, TenantId, "temperature", 41.5, 40.0, RecordedAt);

        await TelemetryThresholdNotificationHandler
            .HandleAsync(message, throttle, publisher, BuildMetrics(), NullLogger<TelemetryThresholdNotificationHandlerCategory>.Instance, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await publisher.DidNotReceive()
            .PublishToEntityFollowersAsync(
                Arg.Any<NotificationType<IoTTelemetryThresholdAlertData>>(),
                Arg.Any<IoTTelemetryThresholdAlertData>(),
                Arg.Any<EntityReference>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task HandleAsync_ThrottleAcquired_ForwardsCancellationToken()
    {
        IAlertThrottle throttle = AcceptingThrottle();
        INotificationPublisher publisher = Substitute.For<INotificationPublisher>();
        using CancellationTokenSource cts = new();

        TelemetryThresholdExceededEto message = new(DeviceId, TenantId, "temperature", 41.5, 40.0, RecordedAt);

        await TelemetryThresholdNotificationHandler
            .HandleAsync(message, throttle, publisher, BuildMetrics(), NullLogger<TelemetryThresholdNotificationHandlerCategory>.Instance, cts.Token)
            .ConfigureAwait(true);

        await publisher.Received(1)
            .PublishToEntityFollowersAsync(
                Arg.Any<NotificationType<IoTTelemetryThresholdAlertData>>(),
                Arg.Any<IoTTelemetryThresholdAlertData>(),
                Arg.Any<EntityReference>(),
                cts.Token)
            .ConfigureAwait(true);
    }

    private static IAlertThrottle AcceptingThrottle()
    {
        IAlertThrottle throttle = Substitute.For<IAlertThrottle>();
        throttle.TryAcquireAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        return throttle;
    }

    private static IoTMetrics BuildMetrics() => new(new EmptyMeterFactory());

    private sealed class EmptyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
