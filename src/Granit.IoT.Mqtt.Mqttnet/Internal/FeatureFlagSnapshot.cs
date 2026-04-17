using Granit.Features;

namespace Granit.IoT.Mqtt.Mqttnet.Internal;

/// <summary>
/// Local TTL cache around <see cref="IFeatureChecker.IsEnabledAsync"/> for the MQTT
/// feature flag. Removes the per-message <c>Task</c> allocation pressure that
/// FusionCache-backed lookup would still incur on the bridge hot loop, while keeping
/// flag flips visible within <see cref="_ttl"/>.
/// </summary>
/// <remarks>
/// Single-key cache (the MQTT feature flag is a deployment-level switch — per-tenant
/// gating happens later in the Wolverine handlers, where the device is already resolved).
/// </remarks>
internal sealed class FeatureFlagSnapshot(IFeatureChecker featureChecker, TimeProvider clock, TimeSpan ttl, string featureName)
    : IDisposable
{
    private readonly TimeSpan _ttl = ttl;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _enabled;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (clock.GetUtcNow() < _expiresAt)
            {
                return _enabled;
            }

            bool current = await featureChecker.IsEnabledAsync(featureName, cancellationToken).ConfigureAwait(false);
            _enabled = current;
            _expiresAt = clock.GetUtcNow().Add(_ttl);
            return current;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
