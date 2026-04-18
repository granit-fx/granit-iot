using Granit.Domain;
using Granit.IoT.Events;
using Granit.Timeline;
using Granit.Workflow.Domain;

namespace Granit.IoT.Domain;

/// <summary>
/// Aggregate root representing a physical or logical IoT device in the tenant's fleet.
/// Owns the lifecycle state machine (Provisioning → Active ↔ Suspended → Decommissioned),
/// the current credential binding, and the last-seen heartbeat timestamp. All mutation
/// flows through behavior methods (<see cref="Activate"/>, <see cref="Suspend"/> etc.)
/// that enforce transitions and raise domain / integration events.
/// </summary>
/// <remarks>
/// Soft-deleted via <see cref="FullAuditedAggregateRoot"/>. Workflow transitions are
/// coordinated by <c>Granit.Workflow</c> (see <see cref="IWorkflowStateful"/>). Domain
/// events are also emitted to the timeline (see <see cref="ITimelined"/>).
/// </remarks>
public sealed class Device : FullAuditedAggregateRoot, IMultiTenant, IWorkflowStateful, ITimelined
{
    private Device() { }

    /// <summary>Manufacturer-supplied serial number. Unique per tenant.</summary>
    public DeviceSerialNumber SerialNumber { get; private set; } = null!;

    /// <summary>Hardware model identifier (e.g. <c>"TempProbe-v2"</c>).</summary>
    public HardwareModel Model { get; private set; } = null!;

    /// <summary>Currently installed firmware version (semver).</summary>
    public FirmwareVersion Firmware { get; private set; } = null!;

    /// <summary>Current lifecycle state. Mutated only via behavior methods that enforce the state machine.</summary>
    public DeviceStatus Status { get; private set; }

    /// <summary>Optional operator-supplied label. Free text, tenant-scoped, sanitized before AI exposure.</summary>
    public string? Label { get; private set; }

    /// <summary>Authentication credential (API key, cert thumbprint, etc.). Secret is encrypted at rest. <c>null</c> for unregistered devices.</summary>
    public DeviceCredential? Credential { get; private set; }

    /// <summary>Timestamp of the last valid heartbeat observed for this device. <c>null</c> before the first report.</summary>
    public DateTimeOffset? LastHeartbeatAt { get; private set; }

    /// <summary>Reason recorded when the device was suspended. Cleared on reactivation.</summary>
    public string? SuspensionReason { get; private set; }

    /// <summary>Free-form operator-supplied tags (e.g. <c>{"location": "warehouse-3"}</c>). <c>null</c> when unset.</summary>
    public Dictionary<string, string>? Tags { get; private set; }

    /// <summary>Tenant that owns this device. <c>null</c> for global/host-owned devices.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Explicit <see cref="IMultiTenant.TenantId"/> implementation so only the
    /// Granit audit interceptor can stamp the tenant on persistence. The public
    /// C# property stays read-only so application code cannot mutate the
    /// tenant binding after construction.
    /// </summary>
    Guid? IMultiTenant.TenantId
    {
        get => TenantId;
        set => TenantId = value;
    }

    static string IWorkflowStateful.StatusPropertyName => nameof(Status);
    static string IWorkflowStateful.WorkflowEntityType => "Device";

    /// <summary>Returns the workflow-entity correlation id (<c>Id</c> as string) for <c>Granit.Workflow</c>.</summary>
    public string GetWorkflowEntityId() => Id.ToString();

    /// <summary>
    /// Factory method — the only supported construction path. Starts the device in
    /// <see cref="DeviceStatus.Provisioning"/> and raises
    /// <see cref="DeviceProvisionedEvent"/> + <see cref="DeviceProvisionedEto"/>.
    /// </summary>
    /// <param name="id">Device identifier (UUID v7). Generated via <c>IGuidGenerator</c> upstream.</param>
    /// <param name="tenantId">Owning tenant, or <c>null</c> for host-owned devices.</param>
    /// <param name="serialNumber">Manufacturer-supplied serial number, unique within the tenant.</param>
    /// <param name="model">Hardware model value object.</param>
    /// <param name="firmware">Initial firmware version.</param>
    /// <param name="label">Optional operator-supplied label.</param>
    /// <param name="credential">Optional initial credential. Typically set later via <see cref="UpdateCredential"/> once provisioning completes.</param>
    /// <returns>A new <see cref="Device"/> in the <see cref="DeviceStatus.Provisioning"/> state.</returns>
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

    /// <summary>
    /// Transitions the device from <see cref="DeviceStatus.Provisioning"/> to
    /// <see cref="DeviceStatus.Active"/>. Raises <see cref="DeviceActivatedEvent"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the device is not in the <c>Provisioning</c> state.</exception>
    public void Activate()
    {
        EnsureStatus(DeviceStatus.Provisioning, nameof(Activate));
        Status = DeviceStatus.Active;
        AddDomainEvent(new DeviceActivatedEvent(Id, SerialNumber, TenantId));
    }

    /// <summary>
    /// Suspends an active device — telemetry ingestion may refuse inbound messages
    /// from suspended devices depending on provider policy. Reversible via
    /// <see cref="Reactivate"/>.
    /// </summary>
    /// <param name="reason">Operator-supplied suspension reason. Required (audit trail).</param>
    /// <exception cref="InvalidOperationException">Thrown if the device is not in the <c>Active</c> state.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="reason"/> is null, empty, or whitespace.</exception>
    public void Suspend(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureStatus(DeviceStatus.Active, nameof(Suspend));

        Status = DeviceStatus.Suspended;
        SuspensionReason = reason;
        AddDomainEvent(new DeviceSuspendedEvent(Id, reason, TenantId));
    }

    /// <summary>
    /// Returns a suspended device to the <see cref="DeviceStatus.Active"/> state and
    /// clears <see cref="SuspensionReason"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the device is not in the <c>Suspended</c> state.</exception>
    public void Reactivate()
    {
        EnsureStatus(DeviceStatus.Suspended, nameof(Reactivate));
        Status = DeviceStatus.Active;
        SuspensionReason = null;
        AddDomainEvent(new DeviceReactivatedEvent(Id, SerialNumber, TenantId));
    }

    /// <summary>
    /// Permanently decommissions the device (terminal state). The device must be suspended
    /// first — direct transition from <see cref="DeviceStatus.Active"/> is refused to
    /// force operators to state a suspension reason for the audit log.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the device is already decommissioned or still active.</exception>
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

    /// <summary>Updates the firmware version (e.g. after an OTA push). Does not validate that the version is higher than the previous one.</summary>
    /// <param name="firmware">New firmware version. Required.</param>
    public void UpdateFirmware(FirmwareVersion firmware)
    {
        ArgumentNullException.ThrowIfNull(firmware);
        Firmware = firmware;
    }

    /// <summary>Updates or clears the operator-supplied label. Pass <c>null</c> to clear.</summary>
    public void UpdateLabel(string? label)
    {
        Label = label;
    }

    /// <summary>Updates or clears the device credential. Typically called once during provisioning, then again on rotation.</summary>
    public void UpdateCredential(DeviceCredential? credential)
    {
        Credential = credential;
    }

    /// <summary>Replaces the tag set (defensive copy). Pass <c>null</c> to clear all tags.</summary>
    public void UpdateTags(IReadOnlyDictionary<string, string>? tags)
    {
        Tags = tags is null ? null : new Dictionary<string, string>(tags);
    }

    /// <summary>Records a heartbeat. Called by the ingestion pipeline after a telemetry point is persisted; does not raise domain events to avoid event-storm on high-frequency devices.</summary>
    /// <param name="at">Server-side timestamp of the heartbeat (not device clock).</param>
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
