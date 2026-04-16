using ArchUnitNET.Domain;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Validates conventions specific to the IoT notifications bridge:
/// notification types must be sealed, providers must be internal, and
/// Wolverine handler classes must be public static partial.
/// </summary>
public sealed class NotificationConventionTests
{
    private const string NotificationsNamespacePrefix = "Granit.IoT.Notifications";
    private const string NotificationDefinitionProviderInterface = "Granit.Notifications.Abstractions.INotificationDefinitionProvider";
    private const string SettingDefinitionProviderInterface = "Granit.Settings.Definitions.ISettingDefinitionProvider";

    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void Notification_types_should_end_with_NotificationType_and_be_sealed()
    {
        IReadOnlyList<Class> notificationTypes = Architecture.Classes
            .Where(c => c.FullName.StartsWith(NotificationsNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("NotificationType", StringComparison.Ordinal))
            .ToList();

        notificationTypes.ShouldNotBeEmpty();

        IEnumerable<Class> unsealed = notificationTypes.Where(c => c.IsSealed != true);

        unsealed.ShouldBeEmpty(
            "Notification types under Granit.IoT.Notifications must be sealed. " +
            $"Violators: {string.Join(", ", unsealed.Select(c => c.FullName))}");
    }

    [Fact]
    public void Notification_definition_providers_should_be_internal()
    {
        IEnumerable<Class> publicProviders = ImplementorsOf(NotificationDefinitionProviderInterface)
            .Where(c => c.FullName.StartsWith(NotificationsNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Visibility == Visibility.Public);

        publicProviders.ShouldBeEmpty(
            "INotificationDefinitionProvider implementations under Granit.IoT.Notifications should be internal. " +
            $"Violators: {string.Join(", ", publicProviders.Select(c => c.FullName))}");
    }

    [Fact]
    public void Setting_definition_providers_should_be_internal()
    {
        IEnumerable<Class> publicProviders = ImplementorsOf(SettingDefinitionProviderInterface)
            .Where(c => c.FullName.StartsWith(NotificationsNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Visibility == Visibility.Public);

        publicProviders.ShouldBeEmpty(
            "ISettingDefinitionProvider implementations under Granit.IoT.Notifications should be internal. " +
            $"Violators: {string.Join(", ", publicProviders.Select(c => c.FullName))}");
    }

    [Fact]
    public void Notification_handlers_should_be_public_static_classes()
    {
        IReadOnlyList<Class> handlers = Architecture.Classes
            .Where(c => c.FullName.StartsWith("Granit.IoT.Notifications.Handlers.", StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToList();

        handlers.ShouldNotBeEmpty();

        IEnumerable<Class> violators = handlers
            .Where(c => c.Visibility != Visibility.Public || c.IsAbstract != true || c.IsSealed != true);

        violators.ShouldBeEmpty(
            "Wolverine handler classes under Granit.IoT.Notifications.Handlers must be public static (abstract + sealed in IL). " +
            $"Violators: {string.Join(", ", violators.Select(c => c.FullName))}");
    }

    private static IEnumerable<Class> ImplementorsOf(string interfaceFullName) =>
        Architecture.Classes
            .Where(c => c.ImplementedInterfaces.Any(i => i.FullName == interfaceFullName));
}
