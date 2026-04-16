#pragma warning disable CA2012 // NSubstitute ValueTask setup — consumed exactly once per call
using System.Text;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Scaleway.Internal;
using Granit.IoT.Ingestion.Scaleway.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Scaleway;

public sealed class ScalewayMessageParserTests
{
    [Fact]
    public async Task ParseAsync_ValidEnvelope_ReturnsParsedBatch()
    {
        ScalewayMessageParser parser = BuildParser();
        string envelope = """
        {
          "topic": "devices/SN-001/sensors",
          "message_id": "msg-abc-123",
          "payload": "eyJ0ZW1wIjoyMi41LCJodW1pZGl0eSI6NDV9",
          "qos": 1,
          "timestamp": "2026-03-28T10:00:00+00:00"
        }
        """;

        ParsedTelemetryBatch batch = await parser
            .ParseAsync(Encoding.UTF8.GetBytes(envelope), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        batch.MessageId.ShouldBe("msg-abc-123");
        batch.DeviceExternalId.ShouldBe("SN-001");
        batch.Source.ShouldBe("scaleway");
        batch.Metrics.Count.ShouldBe(2);
        batch.Metrics["temp"].ShouldBe(22.5);
        batch.Metrics["humidity"].ShouldBe(45);
        batch.RecordedAt.ShouldBe(new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task ParseAsync_MalformedJson_ThrowsParseException()
    {
        ScalewayMessageParser parser = BuildParser();

        await Should.ThrowAsync<IngestionParseException>(async () =>
            await parser.ParseAsync(Encoding.UTF8.GetBytes("not json"), TestContext.Current.CancellationToken)
                .ConfigureAwait(true));
    }

    [Fact]
    public async Task ParseAsync_InvalidBase64Payload_ThrowsParseException()
    {
        ScalewayMessageParser parser = BuildParser();
        string envelope = """
        {
          "topic": "devices/SN-001/sensors",
          "message_id": "msg-1",
          "payload": "%not-base64%",
          "qos": 1,
          "timestamp": "2026-03-28T10:00:00+00:00"
        }
        """;

        await Should.ThrowAsync<IngestionParseException>(async () =>
            await parser.ParseAsync(Encoding.UTF8.GetBytes(envelope), TestContext.Current.CancellationToken)
                .ConfigureAwait(true));
    }

    [Fact]
    public async Task ParseAsync_EmptyMetrics_ThrowsParseException()
    {
        ScalewayMessageParser parser = BuildParser();
        string envelope = """
        {
          "topic": "devices/SN-001/sensors",
          "message_id": "msg-1",
          "payload": "e30=",
          "qos": 1,
          "timestamp": "2026-03-28T10:00:00+00:00"
        }
        """;

        await Should.ThrowAsync<IngestionParseException>(async () =>
            await parser.ParseAsync(Encoding.UTF8.GetBytes(envelope), TestContext.Current.CancellationToken)
                .ConfigureAwait(true));
    }

    [Fact]
    public async Task ParseAsync_UsesConfiguredTopicSegmentIndex()
    {
        ScalewayMessageParser parser = BuildParser(topicSegmentIndex: 0);
        string envelope = """
        {
          "topic": "DEVICE-99/devices/sensors",
          "message_id": "msg-1",
          "payload": "eyJ0ZW1wIjoxfQ==",
          "qos": 1,
          "timestamp": "2026-03-28T10:00:00+00:00"
        }
        """;

        ParsedTelemetryBatch batch = await parser
            .ParseAsync(Encoding.UTF8.GetBytes(envelope), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        batch.DeviceExternalId.ShouldBe("DEVICE-99");
    }

    [Fact]
    public async Task ParseAsync_TopicTooShort_ThrowsParseException()
    {
        ScalewayMessageParser parser = BuildParser(topicSegmentIndex: 5);
        string envelope = """
        {
          "topic": "devices/SN-1",
          "message_id": "msg-1",
          "payload": "eyJ0ZW1wIjoxfQ==",
          "qos": 1,
          "timestamp": "2026-03-28T10:00:00+00:00"
        }
        """;

        await Should.ThrowAsync<IngestionParseException>(async () =>
            await parser.ParseAsync(Encoding.UTF8.GetBytes(envelope), TestContext.Current.CancellationToken)
                .ConfigureAwait(true));
    }

    private static ScalewayMessageParser BuildParser(int topicSegmentIndex = 1)
    {
        IOptionsMonitor<ScalewayIoTOptions> monitor = Substitute.For<IOptionsMonitor<ScalewayIoTOptions>>();
        monitor.CurrentValue.Returns(new ScalewayIoTOptions
        {
            SharedSecret = "x",
            TopicDeviceSegmentIndex = topicSegmentIndex,
        });

        return new ScalewayMessageParser(new ScalewayTopicMapper(monitor));
    }
}
