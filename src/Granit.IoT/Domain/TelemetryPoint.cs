using Granit.Domain;

namespace Granit.IoT.Domain;

/// <summary>
/// A single telemetry reading as persisted by the ingestion pipeline. Append-only —
/// no mutation methods are exposed, deletion is handled exclusively by the
/// <c>iot-telemetry-purge</c> background job using <c>ExecuteDelete()</c> for
/// performance (see CLAUDE.md §6d for the interceptor-bypass policy).
/// </summary>
/// <remarks>
/// Storage model is JSONB — one row per device payload, with GIN on
/// <see cref="Metrics"/> and BRIN on <c>recorded_at</c> (see CLAUDE.md §Key design
/// decisions). Multi-tenancy is enforced by the tenant-id query filter applied via
/// <c>ApplyGranitConventions</c>.
/// </remarks>
public sealed class TelemetryPoint : CreationAuditedEntity, IMultiTenant
{
    /// <summary>Maximum metric key length accepted at ingestion.</summary>
    public const int MaxMetricKeyLength = 64;

    /// <summary>Maximum number of metric keys in a single telemetry point.</summary>
    public const int MaxMetricCount = 64;

    private TelemetryPoint() { }

    /// <summary>Identifier of the device that reported the reading.</summary>
    public Guid DeviceId { get; private set; }

    /// <summary>Device-claimed timestamp for the reading. Trust boundary: the ingestion pipeline should clamp or reject implausible values.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>Flat key→double map of measurements for this point (e.g. <c>{"temperature": 21.4}</c>). Stored as JSONB with a GIN index.</summary>
    public IReadOnlyDictionary<string, double> Metrics { get; private set; } = new Dictionary<string, double>();

    /// <summary>Transport-level message id used for deduplication at ingestion time (Redis-backed, 5-minute TTL). <c>null</c> for legacy rows.</summary>
    public string? MessageId { get; private set; }

    /// <summary>Ingestion source name (e.g. <c>"scaleway"</c>, <c>"aws-sns"</c>, <c>"mqtt"</c>). Used for per-source metrics and provenance.</summary>
    public string? Source { get; private set; }

    /// <summary>
    /// Tenant that owns this telemetry point. Stamped at construction from the
    /// authoritative <c>Device.TenantId</c> binding — never from device-supplied
    /// payload fields.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Explicit <see cref="IMultiTenant.TenantId"/> implementation so only the
    /// Granit audit interceptor can write this field during persistence. The
    /// public C# property remains read-only to callers.
    /// </summary>
    Guid? IMultiTenant.TenantId
    {
        get => TenantId;
        set => TenantId = value;
    }

    /// <summary>
    /// Factory method — the only supported construction path. Defensively copies
    /// <paramref name="metrics"/> so the caller cannot mutate it after persistence,
    /// and enforces the <see cref="MaxMetricCount"/> / <see cref="MaxMetricKeyLength"/>
    /// caps as a cardinality-explosion / PII-dump guard.
    /// </summary>
    /// <param name="id">Telemetry point identifier (UUID v7, time-ordered for BRIN friendliness).</param>
    /// <param name="deviceId">Owning device.</param>
    /// <param name="tenantId">Tenant binding, resolved from the device by the ingestion pipeline — never trusted from payload.</param>
    /// <param name="recordedAt">Device-claimed timestamp.</param>
    /// <param name="metrics">Metric readings. Must contain at least one entry and at most <see cref="MaxMetricCount"/>.</param>
    /// <param name="messageId">Optional transport-level message id for deduplication audit.</param>
    /// <param name="source">Optional ingestion source discriminator.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="metrics"/> is empty, over capacity, or contains an invalid key.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
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

        if (metrics.Count > MaxMetricCount)
        {
            throw new ArgumentException(
                $"Telemetry point rejected: {metrics.Count} metrics exceed the cap of {MaxMetricCount}. " +
                "A single device should not emit unbounded metric keys — cap blocks cardinality-explosion " +
                "and PII-dump attacks via metric keys.", nameof(metrics));
        }

        foreach (string key in metrics.Keys)
        {
            ValidateMetricKey(key);
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

    /// <summary>
    /// Bounded key validation: length + character class. A sensor emitting
    /// <c>"patient_12345_ssn_9"</c> as a metric key gets rejected before the
    /// value reaches JSONB storage or structured logs. Domain-level
    /// <see cref="MetricName"/> enforces the stricter dot-notation shape
    /// where callers want it; this cap is the non-negotiable floor.
    /// </summary>
    private static void ValidateMetricKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Telemetry metric key must not be blank.", nameof(key));
        }

        if (key.Length > MaxMetricKeyLength)
        {
            throw new ArgumentException(
                $"Telemetry metric key '{key[..Math.Min(key.Length, 16)]}…' exceeds the " +
                $"{MaxMetricKeyLength}-character cap.", nameof(key));
        }

        foreach (char c in key)
        {
            if (char.IsControl(c) || char.IsWhiteSpace(c))
            {
                throw new ArgumentException(
                    $"Telemetry metric key contains an illegal control or whitespace character.",
                    nameof(key));
            }
        }
    }
}
