using Granit.IoT.Notifications;
using Granit.IoT.Notifications.Internal;
using Granit.Notifications;
using Granit.Notifications.Abstractions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Notifications.Tests.Notifications;

public sealed class IoTNotificationDefinitionProviderTests
{
    [Fact]
    public void Define_RegistersExactlyTwoDefinitions()
    {
        List<NotificationDefinition> captured = CaptureDefinitions();

        captured.Count.ShouldBe(2);
    }

    [Fact]
    public void Define_TelemetryThresholdAlert_HasExpectedShape()
    {
        NotificationDefinition definition = CaptureDefinitions()
            .Single(d => d.Name == IoTTelemetryThresholdAlertNotificationType.Instance.Name);

        definition.DefaultSeverity.ShouldBe(NotificationSeverity.Warning);
        definition.DefaultChannels.ShouldBe([NotificationChannels.Email, NotificationChannels.Push]);
        definition.AllowUserOptOut.ShouldBeFalse();
        definition.GroupName.ShouldBe("IoT");
    }

    [Fact]
    public void Define_DeviceOffline_HasExpectedShape()
    {
        NotificationDefinition definition = CaptureDefinitions()
            .Single(d => d.Name == IoTDeviceOfflineNotificationType.Instance.Name);

        definition.DefaultSeverity.ShouldBe(NotificationSeverity.Fatal);
        definition.DefaultChannels.ShouldBe([NotificationChannels.Email, NotificationChannels.Push, NotificationChannels.Sms]);
        definition.AllowUserOptOut.ShouldBeFalse();
        definition.GroupName.ShouldBe("IoT");
    }

    private static List<NotificationDefinition> CaptureDefinitions()
    {
        List<NotificationDefinition> captured = [];
        INotificationDefinitionContext context = Substitute.For<INotificationDefinitionContext>();
        context.When(c => c.Add(Arg.Any<NotificationDefinition>()))
            .Do(call => captured.Add(call.Arg<NotificationDefinition>()));

        new IoTNotificationDefinitionProvider().Define(context);
        return captured;
    }
}
