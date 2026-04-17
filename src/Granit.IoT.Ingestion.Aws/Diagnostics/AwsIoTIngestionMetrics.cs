using System.Diagnostics.Metrics;

namespace Granit.IoT.Ingestion.Aws.Diagnostics;

/// <summary>
/// OpenTelemetry counters for the AWS IoT ingestion paths. Each path has its
/// own set of counters so dashboards can isolate an SNS regression from a
/// SigV4 clock-skew spike on the Direct path.
/// </summary>
public sealed class AwsIoTIngestionMetrics : IDisposable
{
    /// <summary>Meter name used by OpenTelemetry exporters.</summary>
    public const string MeterName = "Granit.IoT.Ingestion.Aws";

    private readonly Meter _meter;

    public AwsIoTIngestionMetrics()
    {
        _meter = new Meter(MeterName);
        SnsAccepted = _meter.CreateCounter<long>(
            "granit.iot.aws.ingestion.sns.accepted",
            description: "SNS inbound messages accepted (signature valid, not a replay).");
        SnsRejected = _meter.CreateCounter<long>(
            "granit.iot.aws.ingestion.sns.rejected",
            description: "SNS inbound messages rejected (invalid signature, bad envelope, wrong topic ARN).");
        SnsReplays = _meter.CreateCounter<long>(
            "granit.iot.aws.ingestion.sns.replays",
            description: "SNS messages short-circuited by MessageId deduplication.");
        SnsSubscriptionConfirmations = _meter.CreateCounter<long>(
            "granit.iot.aws.ingestion.sns.subscription_confirmations",
            description: "SNS SubscriptionConfirmation messages processed.");
        SnsCertFetches = _meter.CreateCounter<long>(
            "granit.iot.aws.ingestion.sns.cert_fetches",
            description: "HTTP fetches of the SNS signing certificate (cache misses).");
    }

    /// <summary>Counter: SNS messages accepted.</summary>
    public Counter<long> SnsAccepted { get; }

    /// <summary>Counter: SNS messages rejected.</summary>
    public Counter<long> SnsRejected { get; }

    /// <summary>Counter: SNS messages short-circuited by dedup.</summary>
    public Counter<long> SnsReplays { get; }

    /// <summary>Counter: SNS subscription confirmations observed.</summary>
    public Counter<long> SnsSubscriptionConfirmations { get; }

    /// <summary>Counter: SNS cert fetches (cache misses).</summary>
    public Counter<long> SnsCertFetches { get; }

    public void Dispose() => _meter.Dispose();
}
