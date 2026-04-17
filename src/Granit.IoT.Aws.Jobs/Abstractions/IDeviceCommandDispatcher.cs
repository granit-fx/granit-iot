namespace Granit.IoT.Aws.Jobs.Abstractions;

/// <summary>
/// Dispatches a <see cref="IDeviceCommand"/> to one or more devices via an
/// underlying transport. The first implementation is AWS IoT Jobs; future
/// providers (Azure IoT Hub Direct Methods, custom MQTT) would lift this
/// interface up into a provider-neutral package.
/// </summary>
public interface IDeviceCommandDispatcher
{
    /// <summary>Distinguishing name surfaced in metrics and logs.</summary>
    string DispatcherName { get; }

    /// <summary>
    /// Dispatches the command. Idempotent on
    /// <see cref="IDeviceCommand.CorrelationId"/>: re-issuing the same
    /// command returns the existing AWS Job id without creating a duplicate.
    /// </summary>
    Task<string> DispatchAsync(
        IDeviceCommand command,
        DeviceCommandTarget target,
        CancellationToken cancellationToken);
}
