namespace Granit.IoT.Permissions;

/// <summary>
/// Permission constants for the IoT module.
/// </summary>
public static class IoTPermissions
{
    public const string GroupName = "IoT";

    public static class Devices
    {
        public const string Read = "IoT.Devices.Read";
        public const string Manage = "IoT.Devices.Manage";
    }

    public static class Telemetry
    {
        public const string Read = "IoT.Telemetry.Read";
    }
}
