using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Jobs.Abstractions;
using Granit.IoT.Aws.Shadow.Events;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.Jobs.Handlers;

/// <summary>
/// Bridges the AWS Device Shadow into the AWS IoT Jobs dispatcher: every
/// non-empty <c>delta</c> surfaced by <c>ShadowDeltaPollingService</c>
/// (PR #5) becomes one job dispatched against the originating Thing. The
/// command's <c>operation</c> is fixed at <c>shadow.applyDesiredState</c>;
/// the parameters are the raw delta dictionary.
/// </summary>
public static partial class ShadowDesiredStateCommandHandler
{
    /// <summary>Operation name embedded in the dispatched Job document.</summary>
    public const string OperationName = "shadow.applyDesiredState";

    public static async Task HandleAsync(
        DeviceDesiredStateChangedEvent message,
        IAwsThingBindingReader bindings,
        IDeviceCommandDispatcher dispatcher,
        ILogger<ShadowDesiredStateCommandHandlerCategory> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Delta.Count == 0)
        {
            return;
        }

        AwsThingBinding? binding = await bindings
            .FindByDeviceAsync(message.DeviceId, cancellationToken).ConfigureAwait(false);
        if (binding is null
            || binding.ProvisioningStatus < AwsThingProvisioningStatus.Active
            || string.IsNullOrEmpty(binding.ThingArn))
        {
            // No Active AWS Thing to send the command to.
            return;
        }

        var command = new ShadowDeltaCommand(
            // Deterministic correlationId derived from the shadow version
            // means a re-delivered DeviceDesiredStateChangedEvent dispatches
            // the *same* AWS Job (idempotent reuse instead of duplicate).
            CorrelationIdFor(message.DeviceId, message.ShadowVersion),
            message.TenantId,
            message.Delta);

        string jobId = await dispatcher
            .DispatchAsync(command, DeviceCommandTarget.ForThing(binding.ThingArn!), cancellationToken)
            .ConfigureAwait(false);

        LogDispatched(logger, message.DeviceId, message.ShadowVersion, jobId);
    }

    [LoggerMessage(EventId = 5101, Level = LogLevel.Information,
        Message = "Dispatched shadow delta job '{JobId}' for device {DeviceId} (shadow version {Version}).")]
    private static partial void LogDispatched(ILogger logger, Guid DeviceId, long Version, string JobId);

    private static Guid CorrelationIdFor(Guid deviceId, long shadowVersion)
    {
        // RFC 4122 §4.3 name-based UUID derived from the (deviceId, version)
        // pair — same input, same output, every replay.
        Span<byte> input = stackalloc byte[24];
        deviceId.TryWriteBytes(input[..16]);
        BitConverter.TryWriteBytes(input[16..], shadowVersion);
        Span<byte> hash = stackalloc byte[32];
        System.Security.Cryptography.SHA256.HashData(input, hash);
        return new Guid(hash[..16]);
    }

    private sealed record ShadowDeltaCommand(
        Guid CorrelationId,
        Guid? TenantId,
        IReadOnlyDictionary<string, object?> Parameters)
        : IDeviceCommand
    {
        public string Operation => OperationName;
    }
}

/// <summary>
/// Marker for <see cref="ILogger{TCategoryName}"/> binding.
/// </summary>
public sealed class ShadowDesiredStateCommandHandlerCategory;
