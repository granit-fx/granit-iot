namespace Granit.IoT.Aws.Jobs.Abstractions;

/// <summary>
/// Command sent to one or many AWS IoT Things via the Jobs service.
/// Concrete commands are simple records (e.g. <c>FirmwareUpdateCommand</c>,
/// <c>ConfigUpdateCommand</c>) — the dispatcher serialises them into the Job
/// document the device runtime parses.
/// </summary>
public interface IDeviceCommand
{
    /// <summary>
    /// Stable correlation id used to track the command end-to-end. Becomes
    /// the AWS Job id (<c>granit-{correlationId}</c>), the cache tracking
    /// key, and the value of <c>correlationId</c> inside the Job document.
    /// Must be unique per logical command — re-using the same value with
    /// different parameters bypasses idempotency.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>Operation name embedded in the Job document.</summary>
    string Operation { get; }

    /// <summary>
    /// Tenant the command belongs to (used for telemetry tagging and
    /// downstream event correlation). May be <c>null</c> for global commands.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Free-form parameter bag serialised to JSON inside the Job document.
    /// Implementations should restrict themselves to JSON-friendly values
    /// (string / number / bool / nested dict). The dispatcher doesn't
    /// validate the shape — the device runtime is the contract.
    /// </summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }
}

/// <summary>How an <see cref="IDeviceCommand"/> reaches its target(s).</summary>
public enum DeviceCommandTargetMode
{
    /// <summary>Single device — addressed by Thing ARN.</summary>
    Thing = 0,

    /// <summary>Static ThingGroup — addressed by group ARN (managed out-of-band).</summary>
    ThingGroup = 1,

    /// <summary>
    /// Dynamic ThingGroup — addressed by an MQTT-style query
    /// (<c>attributes.model:THERM-PRO</c>). The dispatcher creates or
    /// reuses a deterministic dynamic group named <c>granit-dynamic-{hash(query)}</c>.
    /// </summary>
    DynamicThingGroup = 2,
}

/// <summary>
/// Where a single <see cref="IDeviceCommand"/> should land.
/// </summary>
public sealed record DeviceCommandTarget(
    DeviceCommandTargetMode Mode,
    string Value)
{
    /// <summary>Single device, addressed via the binding's Thing ARN.</summary>
    public static DeviceCommandTarget ForThing(string thingArn) =>
        new(DeviceCommandTargetMode.Thing, thingArn);

    /// <summary>Static ThingGroup ARN.</summary>
    public static DeviceCommandTarget ForGroup(string thingGroupArn) =>
        new(DeviceCommandTargetMode.ThingGroup, thingGroupArn);

    /// <summary>Dynamic ThingGroup query.</summary>
    public static DeviceCommandTarget ForDynamicQuery(string query) =>
        new(DeviceCommandTargetMode.DynamicThingGroup, query);
}
