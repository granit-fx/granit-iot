using Granit.Authorization;
using Granit.IoT.Endpoints.Permissions;
using Granit.IoT.Permissions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Endpoints.Tests.Permissions;

public sealed class PermissionsTests
{
    [Fact]
    public void DefinePermissions_AddsExpectedGroupAndPermissions()
    {
        IPermissionDefinitionContext ctx = Substitute.For<IPermissionDefinitionContext>();
        PermissionGroup group = new(IoTPermissions.GroupName);
        ctx.AddGroup(IoTPermissions.GroupName).Returns(group);

        IoTEndpointsPermissionDefinitionProvider provider = new();

        provider.DefinePermissions(ctx);

        ctx.Received(1).AddGroup(IoTPermissions.GroupName);
        group.Permissions.Select(p => p.Name).ShouldBe(
            [IoTPermissions.Devices.Read, IoTPermissions.Devices.Manage, IoTPermissions.Telemetry.Read],
            ignoreOrder: true);
    }
}
