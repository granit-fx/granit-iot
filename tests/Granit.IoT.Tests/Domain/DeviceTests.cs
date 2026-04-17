using Granit.IoT.Domain;
using Granit.IoT.Events;
using Shouldly;

namespace Granit.IoT.Tests.Domain;

public sealed class DeviceTests
{
    private static Device CreateProvisioningDevice() =>
        Device.Create(
            Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            DeviceSerialNumber.Create("SN-001"),
            HardwareModel.Create("Sensor-V2"),
            FirmwareVersion.Create("1.0.0"));

    [Fact]
    public void Create_SetsStatusToProvisioning()
    {
        Device device = CreateProvisioningDevice();

        device.Status.ShouldBe(DeviceStatus.Provisioning);
    }

    [Fact]
    public void Create_RaisesDeviceProvisionedEvent()
    {
        Device device = CreateProvisioningDevice();

        device.DomainEvents.ShouldContain(e => e is DeviceProvisionedEvent);
    }

    [Fact]
    public void Create_RaisesDeviceProvisionedEto()
    {
        Device device = CreateProvisioningDevice();

        device.IntegrationEvents.ShouldContain(e => e is DeviceProvisionedEto);
    }

    [Fact]
    public void Activate_FromProvisioning_SetsStatusToActive()
    {
        Device device = CreateProvisioningDevice();

        device.Activate();

        device.Status.ShouldBe(DeviceStatus.Active);
    }

    [Fact]
    public void Activate_RaisesDeviceActivatedEvent()
    {
        Device device = CreateProvisioningDevice();

        device.Activate();

        device.DomainEvents.ShouldContain(e => e is DeviceActivatedEvent);
    }

    [Fact]
    public void Activate_FromActive_Throws()
    {
        Device device = CreateProvisioningDevice();
        device.Activate();

        Should.Throw<InvalidOperationException>(() => device.Activate());
    }

    [Fact]
    public void Suspend_FromActive_SetsStatusToSuspended()
    {
        Device device = CreateProvisioningDevice();
        device.Activate();

        device.Suspend("Maintenance");

        device.Status.ShouldBe(DeviceStatus.Suspended);
        device.SuspensionReason.ShouldBe("Maintenance");
    }

    [Fact]
    public void Suspend_RaisesDeviceSuspendedEvent()
    {
        Device device = CreateProvisioningDevice();
        device.Activate();

        device.Suspend("Testing");

        device.DomainEvents.ShouldContain(e => e is DeviceSuspendedEvent);
    }

    [Fact]
    public void Suspend_FromProvisioning_Throws()
    {
        Device device = CreateProvisioningDevice();

        Should.Throw<InvalidOperationException>(() => device.Suspend("reason"));
    }

    [Fact]
    public void Reactivate_FromSuspended_SetsStatusToActive()
    {
        Device device = CreateProvisioningDevice();
        device.Activate();
        device.Suspend("Maintenance");

        device.Reactivate();

        device.Status.ShouldBe(DeviceStatus.Active);
        device.SuspensionReason.ShouldBeNull();
    }

    [Fact]
    public void Reactivate_RaisesDeviceReactivatedEvent()
    {
        Device device = CreateProvisioningDevice();
        device.Activate();
        device.Suspend("Maintenance");

        device.Reactivate();

        device.DomainEvents.ShouldContain(e => e is DeviceReactivatedEvent);
    }

    [Fact]
    public void Decommission_FromSuspended_SetsStatusToDecommissioned()
    {
        Device device = CreateProvisioningDevice();
        device.Activate();
        device.Suspend("EOL");

        device.Decommission();

        device.Status.ShouldBe(DeviceStatus.Decommissioned);
    }

    [Fact]
    public void Decommission_FromActive_Throws()
    {
        Device device = CreateProvisioningDevice();
        device.Activate();

        Should.Throw<InvalidOperationException>(() => device.Decommission());
    }

    [Fact]
    public void Decommission_FromProvisioning_Succeeds()
    {
        Device device = CreateProvisioningDevice();

        device.Decommission();

        device.Status.ShouldBe(DeviceStatus.Decommissioned);
    }

    [Fact]
    public void Decommission_RaisesDeviceDecommissionedEvent()
    {
        Device device = CreateProvisioningDevice();

        device.Decommission();

        device.DomainEvents.ShouldContain(e => e is DeviceDecommissionedEvent);
    }

    [Fact]
    public void UpdateFirmware_ChangesVersion()
    {
        Device device = CreateProvisioningDevice();

        device.UpdateFirmware(FirmwareVersion.Create("2.0.0"));

        device.Firmware.Value.ShouldBe("2.0.0");
    }

    [Fact]
    public void RecordHeartbeat_SetsLastHeartbeatAt()
    {
        Device device = CreateProvisioningDevice();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        device.RecordHeartbeat(now);

        device.LastHeartbeatAt.ShouldBe(now);
    }

    [Fact]
    public void WorkflowStateful_ReturnsCorrectPropertyName()
    {
        IWorkflowStatefulAccessor.StatusPropertyName<Device>().ShouldBe("Status");
    }

    [Fact]
    public void WorkflowStateful_ReturnsCorrectEntityType()
    {
        IWorkflowStatefulAccessor.WorkflowEntityType<Device>().ShouldBe("Device");
    }
}

internal static class IWorkflowStatefulAccessor
{
    public static string StatusPropertyName<T>() where T : Granit.Workflow.Domain.IWorkflowStateful =>
        T.StatusPropertyName;

    public static string WorkflowEntityType<T>() where T : Granit.Workflow.Domain.IWorkflowStateful =>
        T.WorkflowEntityType;
}
