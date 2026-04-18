#pragma warning disable CA2012 // NSubstitute ValueTask setup — consumed exactly once per call
using System.Diagnostics.Metrics;
using Granit.Domain;
using Granit.IoT.Diagnostics;
using Granit.IoT.Events;
using Granit.IoT.Notifications;
using Granit.IoT.Notifications.Handlers;
using Granit.Notifications;
using Granit.Notifications.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Notifications.Tests.Handlers;

public sealed class DeviceOfflineNotificationHandlerTests
{
    private static readonly Guid DeviceId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTimeOffset LastHeartbeat = new(2026, 4, 16, 11, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_NullMessage_Throws()
    {
        INotificationPublisher publisher = Substitute.For<INotificationPublisher>();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            DeviceOfflineNotificationHandler.HandleAsync(
                null!,
                publisher,
                BuildMetrics(),
                NullLogger<DeviceOfflineNotificationHandlerCategory>.Instance,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_PublishesToEntityFollowersWithDeviceData()
    {
        INotificationPublisher publisher = Substitute.For<INotificationPublisher>();
        DeviceOfflineDetectedEto message = new(DeviceId, "SN-1", LastHeartbeat, TenantId);

        await DeviceOfflineNotificationHandler.HandleAsync(
            message,
            publisher,
            BuildMetrics(),
            NullLogger<DeviceOfflineNotificationHandlerCategory>.Instance,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await publisher.Received(1)
            .PublishToEntityFollowersAsync(
                Arg.Is<NotificationType<IoTDeviceOfflineData>>(t => t == IoTDeviceOfflineNotificationType.Instance),
                Arg.Is<IoTDeviceOfflineData>(d => d.DeviceId == DeviceId && d.LastSeenAt == LastHeartbeat),
                Arg.Is<EntityReference>(e => e.EntityType == "Device" && e.EntityId == DeviceId.ToString("N")),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task HandleAsync_NullLastHeartbeat_UsesMinValue()
    {
        INotificationPublisher publisher = Substitute.For<INotificationPublisher>();
        DeviceOfflineDetectedEto message = new(DeviceId, "SN-1", null, TenantId);

        await DeviceOfflineNotificationHandler.HandleAsync(
            message,
            publisher,
            BuildMetrics(),
            NullLogger<DeviceOfflineNotificationHandlerCategory>.Instance,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await publisher.Received(1)
            .PublishToEntityFollowersAsync(
                Arg.Any<NotificationType<IoTDeviceOfflineData>>(),
                Arg.Is<IoTDeviceOfflineData>(d => d.LastSeenAt == DateTimeOffset.MinValue),
                Arg.Any<EntityReference>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public void IoTDeviceOfflineNotificationType_Singleton_IsConfigured()
    {
        IoTDeviceOfflineNotificationType type = IoTDeviceOfflineNotificationType.Instance;

        type.Name.ShouldBe("IoT.DeviceOffline");
        type.DefaultSeverity.ShouldBe(NotificationSeverity.Fatal);
        type.DefaultChannels.ShouldContain(NotificationChannels.Email);
        type.DefaultChannels.ShouldContain(NotificationChannels.Push);
        type.DefaultChannels.ShouldContain(NotificationChannels.Sms);
    }

    private static IoTMetrics BuildMetrics() => new(new EmptyMeterFactory());

    private sealed class EmptyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
