using Granit.Domain;
using Granit.IoT.Events;
using Granit.Timeline;
using Granit.Workflow.Domain;

namespace Granit.IoT.Domain;

public sealed class Device : FullAuditedAggregateRoot, IMultiTenant, IWorkflowStateful, ITimelined
{
    private Device() { }

    public DeviceSerialNumber SerialNumber { get; private set; } = null!;

    public HardwareModel Model { get; private set; } = null!;

    public FirmwareVersion Firmware { get; private set; } = null!;

    public DeviceStatus Status { get; private set; }

    public string? Label { get; private set; }

    public DeviceCredential? Credential { get; private set; }

    public DateTimeOffset? LastHeartbeatAt { get; private set; }

    public string? SuspensionReason { get; private set; }

    public Dictionary<string, string>? Tags { get; private set; }

    public Guid? TenantId { get; set; }

    static string IWorkflowStateful.StatusPropertyName => nameof(Status);
    static string IWorkflowStateful.WorkflowEntityType => "Device";

    public string GetWorkflowEntityId() => Id.ToString();

    public static Device Create(
        Guid id,
        Guid? tenantId,
        DeviceSerialNumber serialNumber,
        HardwareModel model,
        FirmwareVersion firmware,
        string? label = null,
        DeviceCredential? credential = null)
    {
        var device = new Device
        {
            Id = id,
            TenantId = tenantId,
            SerialNumber = serialNumber,
            Model = model,
            Firmware = firmware,
            Label = label,
            Credential = credential,
            Status = DeviceStatus.Provisioning,
        };

        device.AddDomainEvent(new DeviceProvisionedEvent(id, serialNumber, tenantId));
        device.AddDistributedEvent(new DeviceProvisionedEto(id, serialNumber, model, tenantId));

        return device;
    }

    public void Activate()
    {
        EnsureStatus(DeviceStatus.Provisioning, nameof(Activate));
        Status = DeviceStatus.Active;
    }

    public void Suspend(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureStatus(DeviceStatus.Active, nameof(Suspend));

        Status = DeviceStatus.Suspended;
        SuspensionReason = reason;
        AddDomainEvent(new DeviceSuspendedEvent(Id, reason, TenantId));
    }

    public void Reactivate()
    {
        EnsureStatus(DeviceStatus.Suspended, nameof(Reactivate));
        Status = DeviceStatus.Active;
        SuspensionReason = null;
    }

    public void Decommission()
    {
        if (Status is DeviceStatus.Decommissioned)
        {
            throw new InvalidOperationException("Device is already decommissioned.");
        }

        if (Status is DeviceStatus.Active)
        {
            throw new InvalidOperationException("Device must be suspended before decommission.");
        }

        Status = DeviceStatus.Decommissioned;
        AddDomainEvent(new DeviceDecommissionedEvent(Id, TenantId));
    }

    public void UpdateFirmware(FirmwareVersion firmware)
    {
        ArgumentNullException.ThrowIfNull(firmware);
        Firmware = firmware;
    }

    public void UpdateLabel(string? label)
    {
        Label = label;
    }

    public void UpdateCredential(DeviceCredential? credential)
    {
        Credential = credential;
    }

    public void RecordHeartbeat(DateTimeOffset at)
    {
        LastHeartbeatAt = at;
    }

    private void EnsureStatus(DeviceStatus expected, string operation)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} a device in '{Status}' status. Expected '{expected}'.");
        }
    }
}
