using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Events;
using Shouldly;

namespace Granit.IoT.Aws.Tests.Events;

public sealed class AwsThingEventsTests
{
    [Fact]
    public void AwsThingProvisionedEvent_StoresFields()
    {
        var deviceId = Guid.NewGuid();
        var name = ThingName.Create($"t{Guid.NewGuid():N}-sn1");
        var tenant = Guid.NewGuid();

        AwsThingProvisionedEvent evt = new(deviceId, name, "arn:aws:iot:thing/thing-1", tenant);

        evt.DeviceId.ShouldBe(deviceId);
        evt.ThingName.ShouldBe(name);
        evt.ThingArn.ShouldBe("arn:aws:iot:thing/thing-1");
        evt.TenantId.ShouldBe(tenant);
    }

    [Fact]
    public void AwsThingDecommissionedEvent_StoresFields()
    {
        var deviceId = Guid.NewGuid();
        var name = ThingName.Create($"t{Guid.NewGuid():N}-sn2");

        AwsThingDecommissionedEvent evt = new(deviceId, name, null);

        evt.DeviceId.ShouldBe(deviceId);
        evt.ThingName.ShouldBe(name);
        evt.TenantId.ShouldBeNull();
    }
}
