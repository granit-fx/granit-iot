using System.Text;
using Granit.IoT.Ingestion;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Mqtt.Internal;
using Shouldly;

namespace Granit.IoT.Mqtt.Tests.Parsing;

public sealed class MqttMessageParserTests
{
    private static readonly MqttMessageParser Parser = new();

    [Fact]
    public void SourceName_IsMqtt() => Parser.SourceName.ShouldBe("mqtt");

    [Fact]
    public async Task ParseAsync_HappyPath_ReturnsExpectedBatch()
    {
        const string envelope = """
            {
              "message_id": "client-1:42",
              "topic": "devices/SN-001/telemetry",
              "qos": 1,
              "retain": false,
              "client_id": "client-1",
              "timestamp": "2026-04-17T10:00:00Z",
              "payload": {
                "recordedAt": "2026-04-17T09:59:55Z",
                "metrics": { "temperature": 42.1 },
                "tags": { "fleet": "edge" }
              }
            }
            """;

        ParsedTelemetryBatch batch = await Parser.ParseAsync(
            Encoding.UTF8.GetBytes(envelope),
            TestContext.Current.CancellationToken);

        batch.MessageId.ShouldBe("client-1:42");
        batch.DeviceExternalId.ShouldBe("SN-001");
        batch.RecordedAt.ShouldBe(DateTimeOffset.Parse("2026-04-17T09:59:55Z", System.Globalization.CultureInfo.InvariantCulture));
        batch.Metrics.ShouldContainKeyAndValue("temperature", 42.1);
        batch.Source.ShouldBe("mqtt");
        batch.Tags.ShouldNotBeNull().ShouldContainKeyAndValue("fleet", "edge");
    }

    [Fact]
    public async Task ParseAsync_RecordedAt_FallsBackToEnvelopeTimestamp()
    {
        const string envelope = """
            {
              "message_id": "id",
              "topic": "devices/SN-001/telemetry",
              "timestamp": "2026-04-17T10:00:00Z",
              "payload": { "metrics": { "humidity": 55.0 } }
            }
            """;

        ParsedTelemetryBatch batch = await Parser.ParseAsync(
            Encoding.UTF8.GetBytes(envelope),
            TestContext.Current.CancellationToken);

        batch.RecordedAt.ShouldBe(DateTimeOffset.Parse("2026-04-17T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("""{ "topic": "devices/SN-001/telemetry", "payload": { "metrics": { "x": 1.0 } } }""", "message_id")]
    [InlineData("""{ "message_id": "id", "payload": { "metrics": { "x": 1.0 } } }""", "topic")]
    [InlineData("""{ "message_id": "id", "topic": "devices/SN-001/telemetry" }""", "payload")]
    [InlineData("""{ "message_id": "id", "topic": "devices/SN-001/telemetry", "payload": {} }""", "metric")]
    public async Task ParseAsync_RejectsMissingFields(string envelope, string expectedFragment)
    {
        IngestionParseException ex = await Should.ThrowAsync<IngestionParseException>(async () =>
            await Parser.ParseAsync(Encoding.UTF8.GetBytes(envelope), TestContext.Current.CancellationToken));

        ex.Message.ShouldContain(expectedFragment, Case.Insensitive);
    }

    [Fact]
    public async Task ParseAsync_MalformedJson_Throws()
    {
        await Should.ThrowAsync<IngestionParseException>(async () =>
            await Parser.ParseAsync(Encoding.UTF8.GetBytes("not json"), TestContext.Current.CancellationToken));
    }
}
