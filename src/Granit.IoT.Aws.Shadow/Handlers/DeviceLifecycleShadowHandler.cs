using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Shadow.Abstractions;
using Granit.IoT.Aws.Shadow.Options;
using Granit.IoT.Events;
using Granit.Timing;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Shadow.Handlers;

/// <summary>
/// Wolverine handlers that bridge cloud-agnostic Device lifecycle events
/// into AWS shadow reported updates. Skipped wholesale when
/// <see cref="AwsShadowOptions.AutoPushLifecycleStatus"/> is <c>false</c>.
/// </summary>
public static class DeviceLifecycleShadowHandler
{
    public static Task HandleAsync(
        DeviceActivatedEvent message,
        IAwsThingBindingReader bindings,
        IDeviceShadowSyncService shadow,
        IOptions<AwsShadowOptions> options,
        IClock clock,
        CancellationToken cancellationToken) =>
        PushStatusAsync(message.DeviceId, "Active", bindings, shadow, options, clock, cancellationToken);

    public static Task HandleAsync(
        DeviceSuspendedEvent message,
        IAwsThingBindingReader bindings,
        IDeviceShadowSyncService shadow,
        IOptions<AwsShadowOptions> options,
        IClock clock,
        CancellationToken cancellationToken) =>
        PushStatusAsync(message.DeviceId, "Suspended", bindings, shadow, options, clock, cancellationToken);

    public static Task HandleAsync(
        DeviceReactivatedEvent message,
        IAwsThingBindingReader bindings,
        IDeviceShadowSyncService shadow,
        IOptions<AwsShadowOptions> options,
        IClock clock,
        CancellationToken cancellationToken) =>
        PushStatusAsync(message.DeviceId, "Active", bindings, shadow, options, clock, cancellationToken);

    private static async Task PushStatusAsync(
        Guid deviceId,
        string status,
        IAwsThingBindingReader bindings,
        IDeviceShadowSyncService shadow,
        IOptions<AwsShadowOptions> options,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!options.Value.AutoPushLifecycleStatus)
        {
            return;
        }

        AwsThingBinding? binding = await bindings
            .FindByDeviceAsync(deviceId, cancellationToken).ConfigureAwait(false);
        if (binding is null || binding.ProvisioningStatus < AwsThingProvisioningStatus.Active)
        {
            // No AWS resources to update yet (binding not active or never created).
            return;
        }

        var reported = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["status"] = status,
            ["updatedAt"] = clock.Now.ToString("O"),
        };

        await shadow.PushReportedAsync(binding.ThingName, reported, cancellationToken).ConfigureAwait(false);
    }
}
