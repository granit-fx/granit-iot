using Granit.Authorization;
using Granit.IoT.Permissions;

namespace Granit.IoT.Endpoints.Permissions;

internal sealed class IoTEndpointsPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup(IoTPermissions.GroupName);

        group.AddPermission(IoTPermissions.Devices.Read);
        group.AddPermission(IoTPermissions.Devices.Manage);
        group.AddPermission(IoTPermissions.Telemetry.Read);
    }
}
