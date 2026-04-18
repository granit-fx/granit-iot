namespace Granit.IoT.Domain;

/// <summary>
/// Lifecycle state of a <see cref="Device"/>. Transitions are enforced by the
/// aggregate's behavior methods — see <see cref="Device.Activate"/>,
/// <see cref="Device.Suspend"/>, <see cref="Device.Reactivate"/>,
/// <see cref="Device.Decommission"/>.
/// </summary>
public enum DeviceStatus
{
    /// <summary>Device is registered but has not yet completed initial activation / first heartbeat.</summary>
    Provisioning,

    /// <summary>Device is fully operational — telemetry ingestion accepts its messages.</summary>
    Active,

    /// <summary>Device is temporarily disabled by an operator. Reversible via <see cref="Device.Reactivate"/>.</summary>
    Suspended,

    /// <summary>Terminal state — the device has been removed from service. Cannot be reactivated.</summary>
    Decommissioned,
}
