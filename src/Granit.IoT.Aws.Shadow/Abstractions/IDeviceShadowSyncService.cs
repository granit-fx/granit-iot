using Granit.IoT.Aws.Domain;

namespace Granit.IoT.Aws.Shadow.Abstractions;

/// <summary>
/// Bidirectional bridge between the cloud-agnostic <c>Device</c> aggregate
/// and AWS IoT's Device Shadow document. <see cref="PushReportedAsync"/>
/// publishes a server-truth update; <see cref="GetShadowAsync"/> reads the
/// current document so the polling service can detect
/// <c>desired</c>/<c>reported</c> deltas.
/// </summary>
public interface IDeviceShadowSyncService
{
    /// <summary>
    /// Replaces the shadow's <c>reported</c> block with <paramref name="reported"/>.
    /// AWS merges keys at the top level, so callers can ship only the fields
    /// that changed (e.g. <c>{"status":"Active"}</c>).
    /// </summary>
    Task PushReportedAsync(
        ThingName thingName,
        IReadOnlyDictionary<string, object?> reported,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the current shadow document or <c>null</c> when the Thing
    /// has no shadow yet (no device has ever connected and no <c>reported</c>
    /// has been pushed). The dictionary keys are the top-level fields of
    /// the shadow's <c>state.delta</c> block — empty when there is no
    /// pending desired change.
    /// </summary>
    Task<DeviceShadowSnapshot?> GetShadowAsync(
        ThingName thingName,
        CancellationToken cancellationToken);
}

/// <summary>
/// Read-only snapshot of an AWS IoT Device Shadow.
/// </summary>
/// <param name="Reported">Last reported state (as flat key/value pairs).</param>
/// <param name="Desired">Last desired state (as flat key/value pairs).</param>
/// <param name="Delta">
/// Keys present in <see cref="Desired"/> whose value differs from
/// <see cref="Reported"/>. Empty when the shadow is in sync.
/// </param>
/// <param name="Version">Monotonically increasing AWS shadow version.</param>
public sealed record DeviceShadowSnapshot(
    IReadOnlyDictionary<string, object?> Reported,
    IReadOnlyDictionary<string, object?> Desired,
    IReadOnlyDictionary<string, object?> Delta,
    long Version);
