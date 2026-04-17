using System.Text;
using System.Text.Json;
using Granit.IoT.Ingestion.Abstractions;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Parses the AWS SNS HTTP-subscription envelope: the outer envelope carries
/// a stringified <c>Message</c> field whose content is the payload emitted
/// by the IoT Rule. This parser strips the envelope, decodes the inner JSON,
/// and delegates to the same
/// <see cref="AwsIoTRulePayloadParser.Parse(ReadOnlyMemory{byte}, string)"/>
/// used by the Direct and API Gateway paths.
/// </summary>
/// <remarks>
/// The SNS <c>MessageId</c> is already visible to the signature validator
/// for replay protection. Business-level dedup (via <c>ParsedTelemetryBatch.MessageId</c>)
/// uses the inner <c>messageId</c> emitted by the IoT Rule — if the same
/// device message is redelivered it will be skipped regardless of the
/// SNS redelivery count.
/// </remarks>
internal sealed class AwsIoTSnsPayloadParser : IInboundMessageParser
{
    public string SourceName => AwsIoTIngestionConstants.SnsSourceName;

    public ValueTask<ParsedTelemetryBatch> ParseAsync(
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        SnsEnvelope? envelope = DeserializeSnsEnvelope(body.Span);
        if (envelope is null)
        {
            throw new IngestionParseException("SNS envelope is not valid JSON.");
        }

        // SubscriptionConfirmation and UnsubscribeConfirmation carry no
        // telemetry — the signature validator already handled them. Short-circuit
        // with a zero-metric batch that the pipeline will treat as a parse
        // failure if it ever reaches here.
        if (!string.Equals(envelope.Type, SnsEnvelope.MessageTypes.Notification, StringComparison.Ordinal))
        {
            throw new IngestionParseException(
                $"SNS message Type='{envelope.Type}' carries no telemetry payload — ignore at the endpoint.");
        }

        if (string.IsNullOrEmpty(envelope.Message))
        {
            throw new IngestionParseException("SNS Notification 'Message' field is empty.");
        }

        byte[] innerBytes = Encoding.UTF8.GetBytes(envelope.Message);
        ParsedTelemetryBatch inner = AwsIoTRulePayloadParser.Parse(innerBytes, SourceName);
        return ValueTask.FromResult(inner);
    }

    private static SnsEnvelope? DeserializeSnsEnvelope(ReadOnlySpan<byte> body)
    {
        try
        {
            return JsonSerializer.Deserialize<SnsEnvelope>(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
