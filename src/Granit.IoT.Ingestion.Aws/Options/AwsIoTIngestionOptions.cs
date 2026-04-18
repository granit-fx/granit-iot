using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Ingestion.Aws.Options;

/// <summary>
/// Configures the three AWS IoT Core ingestion paths. Each sub-section is
/// independently toggled via <c>Enabled</c>; at least one path must be enabled.
/// Cross-field rules (region-required, no-api-key-outside-development) are
/// enforced by <c>AwsIoTIngestionOptionsValidator</c>.
/// </summary>
public sealed class AwsIoTIngestionOptions
{
    /// <summary>Configuration binding section: <c>IoT:Ingestion:Aws</c>.</summary>
    public const string SectionName = "IoT:Ingestion:Aws";

    /// <summary>SNS → HTTP subscription path options.</summary>
    public AwsIoTSnsIngestionOptions Sns { get; set; } = new();

    /// <summary>Direct HTTP path options (Bearer API key or SigV4).</summary>
    public AwsIoTDirectIngestionOptions Direct { get; set; } = new();

    /// <summary>API Gateway HTTP path options (SigV4).</summary>
    public AwsIoTApiGatewayIngestionOptions ApiGateway { get; set; } = new();

    /// <summary>
    /// How long to cache a derived SigV4 signing key (HMAC chain over secret
    /// key + date + region + service). Signing keys change once per calendar
    /// day per scope, so 24 hours is the natural default — set lower to
    /// amortize key rotation more aggressively.
    /// </summary>
    [Range(1, 48)]
    public int SigningKeyCacheHours { get; set; } = 24;
}

/// <summary>
/// SNS subscription ingestion path. Messages arrive as SNS envelopes signed by
/// AWS CDN-hosted RSA certificates.
/// </summary>
public sealed class AwsIoTSnsIngestionOptions
{
    /// <summary>Whether the SNS path accepts inbound requests.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// AWS region hosting the SNS topic — used to build the expected
    /// <c>SigningCertURL</c> host. Required when <see cref="Enabled"/> is <c>true</c>.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Optional <c>TopicArn</c> prefix filter. When set, only SNS messages whose
    /// <c>TopicArn</c> starts with this prefix are accepted (fast-fail before RSA
    /// verification). Pin this to your app's IoT topic namespace to block cross-account
    /// noise.
    /// </summary>
    public string? TopicArnPrefix { get; set; }

    /// <summary>
    /// Auto-confirm <c>SubscriptionConfirmation</c> messages by firing a background
    /// HTTP GET to the <c>SubscribeURL</c>. Leave <c>false</c> to require out-of-band
    /// confirmation by ops.
    /// </summary>
    public bool AutoConfirmSubscription { get; set; }

    /// <summary>How long to cache the fetched signing certificate, in hours. Default 24.</summary>
    [Range(1, 168)]
    public int CertCacheHours { get; set; } = 24;

    /// <summary>
    /// Replay-protection window for <c>MessageId</c> deduplication, in minutes.
    /// Default 5 (matches SNS at-least-once delivery SLA).
    /// </summary>
    [Range(1, 60)]
    public int DeduplicationWindowMinutes { get; set; } = 5;
}

/// <summary>
/// Direct HTTP ingestion path. IoT Rules post to the application endpoint with
/// either a Bearer API key (non-Production) or SigV4 (Production).
/// </summary>
public sealed class AwsIoTDirectIngestionOptions
{
    /// <summary>Whether the direct path accepts inbound requests.</summary>
    public bool Enabled { get; set; }

    /// <summary>Authentication mode — defaults to <see cref="DirectAuthMode.SigV4"/>.</summary>
    public DirectAuthMode AuthMode { get; set; } = DirectAuthMode.SigV4;

    /// <summary>
    /// AWS region — required when <see cref="Enabled"/> is <c>true</c> (needed for
    /// both the SigV4 scope and the region-scoped SNS CDN URL pattern in shared code).
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Static Bearer API key for <see cref="DirectAuthMode.ApiKey"/>. Must be
    /// <c>null</c> outside the <c>Development</c> environment — enforced by the
    /// options validator. Load from <c>Granit.Vault</c> in production.
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>API Gateway HTTP ingestion path. SigV4 only.</summary>
public sealed class AwsIoTApiGatewayIngestionOptions
{
    /// <summary>Whether the API Gateway path accepts inbound requests.</summary>
    public bool Enabled { get; set; }

    /// <summary>AWS region — required when <see cref="Enabled"/> is <c>true</c>.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Expected API Gateway stage name (e.g. <c>"prod"</c>). Left empty, any stage
    /// is accepted as long as the SigV4 signature validates.
    /// </summary>
    public string? Stage { get; set; }
}

/// <summary>Authentication mode for the direct HTTP ingestion path.</summary>
public enum DirectAuthMode
{
    /// <summary>SigV4 (AWS4-HMAC-SHA256) — production default.</summary>
    SigV4 = 0,

    /// <summary>Static Bearer API key — development only.</summary>
    ApiKey = 1,
}
