using Granit.Caching;
using Granit.IoT.Notifications.Abstractions;
using Granit.IoT.Notifications.Internal;
using Granit.Modularity;
using Granit.Notifications;
using Granit.Notifications.Abstractions;
using Granit.Settings;
using Granit.Settings.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Notifications;

/// <summary>
/// Bridge between the IoT integration events and Granit.Notifications.
/// Registers the notification + setting definition providers and the alert
/// throttle so a Wolverine handler can map <c>TelemetryThresholdExceededEto</c>
/// to multi-channel user notifications.
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitNotificationsAbstractionsModule))]
[DependsOn(typeof(GranitSettingsModule))]
[DependsOn(typeof(GranitCachingModule))]
public sealed class GranitIoTNotificationsModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddSingleton<INotificationDefinitionProvider, IoTNotificationDefinitionProvider>();
        context.Services.AddSingleton<ISettingDefinitionProvider, IoTSettingDefinitionProvider>();
        context.Services.TryAddScoped<IAlertThrottle, AlertThrottle>();
    }
}
