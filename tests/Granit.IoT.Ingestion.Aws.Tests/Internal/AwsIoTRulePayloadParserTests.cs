using System.Text;
using Granit.IoT.Ingestion;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws.Internal;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal;

public class AwsIoTRulePayloadParserTests
{
    [Fact]
    public async Task Parses_well_formed_envelope_with_iso_timestamp()
    {
        byte[] body = Encoding.UTF8.GetBytes(
            """
            {
                "messageId": "msg-1",
                "deviceId": "SN-001",
                "timestamp": "2026-04-17T12:00:00.000Z",
                "metrics": { "temperature": 22.5, "humidity": 60 }
            }
            """);

        AwsIoTRulePayloadParser parser = new("awsiotdirect");
        ParsedTelemetryBatch batch = await parser.ParseAsync(body, TestContext.Current.CancellationToken);

        batch.MessageId.ShouldBe("msg-1");
        batch.DeviceExternalId.ShouldBe("SN-001");
        batch.RecordedAt.ShouldBe(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        batch.Metrics["temperature"].ShouldBe(22.5);
        batch.Metrics["humidity"].ShouldBe(60);
        batch.Source.ShouldBe("awsiotdirect");
    }

    [Fact]
    public async Task Parses_envelope_with_unix_millisecond_timestamp()
    {
        DateTimeOffset expected = new(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);
        long ms = expected.ToUnixTimeMilliseconds();
        byte[] body = Encoding.UTF8.GetBytes(
            $$"""
            {
                "messageId": "msg-2",
                "deviceId": "SN-002",
                "timestamp": {{ms}},
                "metrics": { "voltage": 3.3 }
            }
            """);

        AwsIoTRulePayloadParser parser = new("awsiotapigw");
        ParsedTelemetryBatch batch = await parser.ParseAsync(body, TestContext.Current.CancellationToken);

        batch.RecordedAt.ShouldBe(expected);
    }

    [Theory]
    [InlineData("""{"deviceId":"SN-1","timestamp":"2026-04-17T12:00:00Z","metrics":{"t":1}}""", "messageId")]
    [InlineData("""{"messageId":"msg","timestamp":"2026-04-17T12:00:00Z","metrics":{"t":1}}""", "deviceId")]
    [InlineData("""{"messageId":"msg","deviceId":"SN-1","timestamp":"2026-04-17T12:00:00Z"}""", "metrics")]
    [InlineData("""{"messageId":"msg","deviceId":"SN-1","timestamp":"2026-04-17T12:00:00Z","metrics":{}}""", "metrics")]
    public async Task Missing_or_empty_required_fields_raise_IngestionParseException(string json, string expectedField)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        AwsIoTRulePayloadParser parser = new("awsiotdirect");

        IngestionParseException ex = await Should.ThrowAsync<IngestionParseException>(
            () => parser.ParseAsync(body, TestContext.Current.CancellationToken).AsTask());
        ex.Message.ShouldContain(expectedField);
    }

    [Fact]
    public async Task Malformed_JSON_raises_IngestionParseException()
    {
        byte[] body = Encoding.UTF8.GetBytes("{ not really json");
        AwsIoTRulePayloadParser parser = new("awsiotdirect");

        IngestionParseException ex = await Should.ThrowAsync<IngestionParseException>(
            () => parser.ParseAsync(body, TestContext.Current.CancellationToken).AsTask());
        ex.Message.ShouldContain("not valid JSON");
    }

    [Fact]
    public async Task Timestamp_with_unsupported_kind_raises_IngestionParseException()
    {
        byte[] body = Encoding.UTF8.GetBytes(
            """{"messageId":"m","deviceId":"d","timestamp":true,"metrics":{"t":1}}""");
        AwsIoTRulePayloadParser parser = new("awsiotdirect");

        IngestionParseException ex = await Should.ThrowAsync<IngestionParseException>(
            () => parser.ParseAsync(body, TestContext.Current.CancellationToken).AsTask());
        ex.Message.ShouldContain("timestamp");
    }
}
