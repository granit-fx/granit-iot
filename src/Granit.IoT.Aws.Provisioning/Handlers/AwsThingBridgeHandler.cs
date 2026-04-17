using Granit.Guids;
using Granit.IoT.Abstractions;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Provisioning.Abstractions;
using Granit.IoT.Aws.Provisioning.Diagnostics;
using Granit.IoT.Aws.Provisioning.Internal;
using Granit.IoT.Domain;
using Granit.IoT.Events;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.Provisioning.Handlers;

/// <summary>
/// Wolverine handlers that translate cloud-agnostic Device lifecycle events
/// into the AWS-side provisioning saga. Reservation + each saga step are
/// idempotent: re-delivery (Wolverine's at-least-once contract) re-enters
/// the handler harmlessly. The first persisted checkpoint a replay sees
/// determines where work resumes.
/// </summary>
public static partial class AwsThingBridgeHandler
{
    /// <summary>
    /// Reacts to <see cref="DeviceProvisionedEvent"/> by walking the
    /// <see cref="AwsThingBinding"/> through Pending → Active.
    /// </summary>
    public static async Task HandleAsync(
        DeviceProvisionedEvent message,
        IAwsThingBindingReader bindings,
        IAwsThingBindingWriter writer,
        IDeviceReader devices,
        IThingProvisioningService provisioning,
        IGuidGenerator guidGenerator,
        AwsProvisioningMetrics metrics,
        ILogger<AwsThingBridgeHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        AwsThingBinding? binding = await bindings
            .FindByDeviceAsync(message.DeviceId, cancellationToken).ConfigureAwait(false);

        if (binding is null)
        {
            binding = await ReserveAsync(message, devices, writer, guidGenerator, logger, cancellationToken)
                .ConfigureAwait(false);
            if (binding is null)
            {
                return;
            }
        }

        // JITP path: AWS already created the Thing; bypass the standard saga
        // and let the event flow through (no-op).
        if (binding.ProvisionedViaJitp || binding.ProvisioningStatus is AwsThingProvisioningStatus.Active)
        {
            return;
        }

        try
        {
            await provisioning.EnsureThingAsync(binding, cancellationToken).ConfigureAwait(false);
            await writer.UpdateAsync(binding, cancellationToken).ConfigureAwait(false);

            await provisioning.EnsureCertificateAndSecretAsync(binding, cancellationToken).ConfigureAwait(false);
            await writer.UpdateAsync(binding, cancellationToken).ConfigureAwait(false);

            await provisioning.EnsureActivationAsync(binding, cancellationToken).ConfigureAwait(false);
            await writer.UpdateAsync(binding, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Wolverine's retry policy will replay this message (which the
            // saga handles idempotently). MarkAsFailed is reserved for cases
            // where the provisioning service itself decided no replay can
            // recover (e.g. unrecoverable cert+secret split). Persist the
            // current binding state so the next replay starts from the right
            // checkpoint.
            await writer.UpdateAsync(binding, cancellationToken).ConfigureAwait(false);
            if (binding.ProvisioningStatus is AwsThingProvisioningStatus.Failed)
            {
                metrics.RecordFailed(binding.TenantId);
                ProvisioningLog.ProvisioningFailed(logger, binding.ThingName.Value, ex);
            }
            throw;
        }
    }

    /// <summary>
    /// Reacts to <see cref="DeviceDecommissionedEvent"/> by walking the AWS
    /// teardown (detach principal/policy, deactivate + delete certificate,
    /// delete secret, delete Thing). Idempotent.
    /// </summary>
    public static async Task HandleAsync(
        DeviceDecommissionedEvent message,
        IAwsThingBindingReader bindings,
        IAwsThingBindingWriter writer,
        IThingProvisioningService provisioning,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        AwsThingBinding? binding = await bindings
            .FindByDeviceAsync(message.DeviceId, cancellationToken).ConfigureAwait(false);
        if (binding is null || binding.ProvisioningStatus is AwsThingProvisioningStatus.Decommissioned)
        {
            return;
        }

        await provisioning.DecommissionAsync(binding, cancellationToken).ConfigureAwait(false);
        await writer.DeleteAsync(binding, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<AwsThingBinding?> ReserveAsync(
        DeviceProvisionedEvent message,
        IDeviceReader devices,
        IAwsThingBindingWriter writer,
        IGuidGenerator guidGenerator,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        Device? device = await devices.FindAsync(message.DeviceId, cancellationToken).ConfigureAwait(false);
        if (device is null)
        {
            // Race: the originating Device was deleted between commit and
            // Wolverine dispatch. Nothing to provision.
            ProvisioningLog.ReservationFailed(logger, message.DeviceId);
            return null;
        }

        var thingName = ThingName.From(message.TenantId ?? Guid.Empty, device.SerialNumber.Value);
        var binding = AwsThingBinding.Create(message.DeviceId, message.TenantId, thingName);
        binding.Id = guidGenerator.Create();

        await writer.AddAsync(binding, cancellationToken).ConfigureAwait(false);
        return binding;
    }
}

/// <summary>
/// Marker type giving <see cref="ILogger{TCategoryName}"/> a stable category
/// for the static <see cref="AwsThingBridgeHandler"/>.
/// </summary>
public sealed class AwsThingBridgeHandlerCategory;
