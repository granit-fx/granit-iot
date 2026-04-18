namespace Granit.IoT.Permissions;

/// <summary>
/// Permission constants for the IoT module. Three-segment naming
/// (<c>IoT.{Resource}.{Action}</c>) consumed by <c>Granit.Authorization</c> and by
/// the permission definition provider (see CLAUDE.md §3a).
/// </summary>
public static class IoTPermissions
{
    /// <summary>Group name used to register IoT permissions with <c>Granit.Authorization</c>.</summary>
    public const string GroupName = "IoT";

    /// <summary>Permissions scoped to the <see cref="Domain.Device"/> aggregate.</summary>
    public static class Devices
    {
        /// <summary>Read / list devices within the current tenant.</summary>
        public const string Read = "IoT.Devices.Read";

        /// <summary>Provision, update, suspend, reactivate, and decommission devices. Implies <see cref="Read"/>.</summary>
        public const string Manage = "IoT.Devices.Manage";
    }

    /// <summary>Permissions scoped to <see cref="Domain.TelemetryPoint"/> history.</summary>
    public static class Telemetry
    {
        /// <summary>Read telemetry history and aggregates for devices in the current tenant.</summary>
        public const string Read = "IoT.Telemetry.Read";
    }
}
