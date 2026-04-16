using Granit.Domain;

namespace Granit.IoT.Domain;

public sealed class TelemetryPoint : CreationAuditedEntity, IMultiTenant
{
    private TelemetryPoint() { }

    public Guid DeviceId { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public IReadOnlyDictionary<string, double> Metrics { get; private set; } = new Dictionary<string, double>();

    public string? MessageId { get; private set; }

    public string? Source { get; private set; }

    public Guid? TenantId { get; set; }

    public static TelemetryPoint Create(
        Guid id,
        Guid deviceId,
        Guid? tenantId,
        DateTimeOffset recordedAt,
        IReadOnlyDictionary<string, double> metrics,
        string? messageId = null,
        string? source = null)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        if (metrics.Count == 0)
        {
            throw new ArgumentException("Metrics dictionary must contain at least one entry.", nameof(metrics));
        }

        return new TelemetryPoint
        {
            Id = id,
            DeviceId = deviceId,
            TenantId = tenantId,
            RecordedAt = recordedAt,
            Metrics = new Dictionary<string, double>(metrics),
            MessageId = messageId,
            Source = source,
        };
    }
}
