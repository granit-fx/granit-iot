using System.Text;
using System.Text.Json;
using Granit.IoT.Ingestion;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws.Internal;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal;

public class AwsIoTSnsPayloadParserTests
{
    [Fact]
    public async Task Unwraps_the_SNS_envelope_and_parses_the_inner_IoT_Rule_payload()
    {
        string innerMessage = JsonSerializer.Serialize(new
        {
            messageId = "iot-rule-1",
            deviceId = "SN-777",
            timestamp = "2026-04-17T12:00:00.000Z",
            metrics = new { temperature = 4.2, humidity = 55 },
        });

        string snsEnvelope = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "sns-msg-1",
            TopicArn = "arn:aws:sns:eu-west-1:1:iot-telemetry",
            Message = innerMessage,
            Timestamp = "2026-04-17T12:00:01.000Z",
            SignatureVersion = "1",
            Signature = "ignored-at-parse-time",
            SigningCertURL = "https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-x.pem",
        });

        AwsIoTSnsPayloadParser parser = new();
        ParsedTelemetryBatch batch = await parser.ParseAsync(
            Encoding.UTF8.GetBytes(snsEnvelope), TestContext.Current.CancellationToken);

        batch.MessageId.ShouldBe("iot-rule-1");
        batch.DeviceExternalId.ShouldBe("SN-777");
        batch.Metrics["temperature"].ShouldBe(4.2);
        batch.Source.ShouldBe("awsiotsns");
    }

    [Fact]
    public async Task SubscriptionConfirmation_is_not_parseable_telemetry()
    {
        string envelope = JsonSerializer.Serialize(new
        {
            Type = "SubscriptionConfirmation",
            MessageId = "conf-1",
            TopicArn = "arn",
            Token = "tok",
            Message = "Please confirm",
            SubscribeURL = "https://example.amazonaws.com/?Action=ConfirmSubscription",
            Timestamp = "2026-04-17T12:00:00.000Z",
            SignatureVersion = "1",
            Signature = "x",
            SigningCertURL = "https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-x.pem",
        });

        AwsIoTSnsPayloadParser parser = new();
        IngestionParseException ex = await Should.ThrowAsync<IngestionParseException>(
            () => parser.ParseAsync(Encoding.UTF8.GetBytes(envelope), TestContext.Current.CancellationToken).AsTask());
        ex.Message.ShouldContain("SubscriptionConfirmation");
    }

    [Fact]
    public async Task Notification_with_empty_Message_is_rejected()
    {
        string envelope = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "sns-1",
            TopicArn = "arn",
            Message = "",
            Timestamp = "2026-04-17T12:00:00.000Z",
            SignatureVersion = "1",
            Signature = "x",
            SigningCertURL = "https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-x.pem",
        });

        AwsIoTSnsPayloadParser parser = new();
        IngestionParseException ex = await Should.ThrowAsync<IngestionParseException>(
            () => parser.ParseAsync(Encoding.UTF8.GetBytes(envelope), TestContext.Current.CancellationToken).AsTask());
        ex.Message.ShouldContain("Message");
    }

    [Fact]
    public async Task Malformed_SNS_envelope_is_rejected()
    {
        AwsIoTSnsPayloadParser parser = new();
        IngestionParseException ex = await Should.ThrowAsync<IngestionParseException>(
            () => parser.ParseAsync(Encoding.UTF8.GetBytes("{ not json"),
                TestContext.Current.CancellationToken).AsTask());
        ex.Message.ShouldContain("not valid JSON");
    }
}
