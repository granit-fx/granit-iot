#pragma warning disable CA2012 // NSubstitute ValueTask setup — consumed exactly once per call
using System.Diagnostics.Metrics;
using System.Text;
using Granit.Events;
using Granit.IoT.Abstractions;
using Granit.IoT.Diagnostics;
using Granit.IoT.Events;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Internal;
using Granit.Timing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Pipeline;

public sealed class IngestionPipelineTests
{
    private const string Source = "test";
    private static readonly DateTimeOffset Now = new(2026, 4, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly ReadOnlyMemory<byte> Body = new byte[] { 1, 2, 3 };

    [Fact]
    public async Task ProcessAsync_UnknownSource_ReturnsUnknownSourceWithoutValidating()
    {
        IInboundMessageDeduplicator deduplicator = AcceptingDeduplicator();
        IngestionPipeline pipeline = BuildPipeline(deduplicator: deduplicator);

        IngestionResult result = await pipeline
            .ProcessAsync("unknown-source", ReadOnlyMemory<byte>.Empty, EmptyHeaders, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(IngestionOutcome.UnknownSource);
        await deduplicator.DidNotReceive()
            .TryAcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ProcessAsync_InvalidSignature_ReturnsSignatureRejected()
    {
        IPayloadSignatureValidator validator = SubstituteValidator(SignatureValidationResult.Invalid("bad"));
        IDistributedEventBus eventBus = Substitute.For<IDistributedEventBus>();

        IngestionPipeline pipeline = BuildPipeline(validator: validator, eventBus: eventBus);

        IngestionResult result = await pipeline
            .ProcessAsync(Source, Body, EmptyHeaders, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(IngestionOutcome.SignatureRejected);
        await eventBus.DidNotReceive()
            .PublishAsync(Arg.Any<TelemetryIngestedEto>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateMessageId_SkipsPublish()
    {
        IInboundMessageDeduplicator deduplicator = Substitute.For<IInboundMessageDeduplicator>();
        deduplicator.TryAcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        IDistributedEventBus eventBus = Substitute.For<IDistributedEventBus>();

        IngestionPipeline pipeline = BuildPipeline(deduplicator: deduplicator, eventBus: eventBus);

        IngestionResult result = await pipeline
            .ProcessAsync(Source, Body, EmptyHeaders, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(IngestionOutcome.Accepted);
        await eventBus.DidNotReceive()
            .PublishAsync(Arg.Any<TelemetryIngestedEto>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await eventBus.DidNotReceive()
            .PublishAsync(Arg.Any<DeviceUnknownEto>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ProcessAsync_UnknownDevice_PublishesDeviceUnknownEto()
    {
        IDeviceLookup deviceLookup = Substitute.For<IDeviceLookup>();
        deviceLookup.FindBySerialNumberAsync("SN-1", Arg.Any<CancellationToken>())
            .Returns((DeviceLookupResult?)null);
        IDistributedEventBus eventBus = Substitute.For<IDistributedEventBus>();

        IngestionPipeline pipeline = BuildPipeline(eventBus: eventBus, deviceLookup: deviceLookup);

        IngestionResult result = await pipeline
            .ProcessAsync(Source, Body, EmptyHeaders, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(IngestionOutcome.Accepted);
        await eventBus.Received(1)
            .PublishAsync(
                Arg.Is<DeviceUnknownEto>(e => e.DeviceExternalId == "SN-1" && e.MessageId == "msg-1"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await eventBus.DidNotReceive()
            .PublishAsync(Arg.Any<TelemetryIngestedEto>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ProcessAsync_ResolvedDevice_PublishesTelemetryIngestedEto()
    {
        var deviceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        IDeviceLookup deviceLookup = Substitute.For<IDeviceLookup>();
        deviceLookup.FindBySerialNumberAsync("SN-1", Arg.Any<CancellationToken>())
            .Returns(new DeviceLookupResult(deviceId, tenantId));
        IDistributedEventBus eventBus = Substitute.For<IDistributedEventBus>();

        IngestionPipeline pipeline = BuildPipeline(eventBus: eventBus, deviceLookup: deviceLookup);

        IngestionResult result = await pipeline
            .ProcessAsync(Source, Body, EmptyHeaders, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(IngestionOutcome.Accepted);
        await eventBus.Received(1)
            .PublishAsync(
                Arg.Is<TelemetryIngestedEto>(e =>
                    e.DeviceId == deviceId
                    && e.TenantId == tenantId
                    && e.MessageId == "msg-1"
                    && e.Source == Source),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ProcessAsync_ParserThrows_ReturnsParseFailure()
    {
        IInboundMessageParser parser = Substitute.For<IInboundMessageParser>();
        parser.SourceName.Returns(Source);
        parser.ParseAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .ThrowsAsyncForAnyArgs(new IngestionParseException("bad json"));

        IInboundMessageDeduplicator deduplicator = AcceptingDeduplicator();
        IDistributedEventBus eventBus = Substitute.For<IDistributedEventBus>();

        IngestionPipeline pipeline = BuildPipeline(parser: parser, deduplicator: deduplicator, eventBus: eventBus);

        IngestionResult result = await pipeline
            .ProcessAsync(Source, Encoding.UTF8.GetBytes("{}"), EmptyHeaders, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(IngestionOutcome.ParseFailure);
        await deduplicator.DidNotReceive()
            .TryAcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Theory]
    [InlineData("msg!@#abc", "msg-abc")]
    [InlineData("ABC-123_xyz", "ABC-123-xyz")]
    [InlineData("a..b//c", "a-b-c")]
    public void SanitizeMessageId_CollapsesNonAlphanumericRunsToSingleDash(string input, string expected)
    {
        IngestionPipeline.SanitizeMessageId(input).ShouldBe(expected);
    }

    [Fact]
    public void SanitizeMessageId_TruncatesAtMaxLength()
    {
        string longId = new('a', 200);
        string result = IngestionPipeline.SanitizeMessageId(longId);
        result.Length.ShouldBe(128);
    }

    private static IReadOnlyDictionary<string, string> EmptyHeaders => new Dictionary<string, string>();

    private static IInboundMessageDeduplicator AcceptingDeduplicator()
    {
        IInboundMessageDeduplicator dedup = Substitute.For<IInboundMessageDeduplicator>();
        dedup.TryAcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        return dedup;
    }

    private static IInboundMessageParser DefaultParser()
    {
        IInboundMessageParser parser = Substitute.For<IInboundMessageParser>();
        parser.SourceName.Returns(Source);
        parser.ParseAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ParsedTelemetryBatch>(new ParsedTelemetryBatch(
                MessageId: "msg-1",
                DeviceExternalId: "SN-1",
                RecordedAt: Now,
                Metrics: new Dictionary<string, double> { ["temperature"] = 22.5 },
                Source: Source)));
        return parser;
    }

    private static IPayloadSignatureValidator SubstituteValidator(SignatureValidationResult result)
    {
        IPayloadSignatureValidator validator = Substitute.For<IPayloadSignatureValidator>();
        validator.SourceName.Returns(Source);
        validator.ValidateAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<SignatureValidationResult>(result));
        return validator;
    }

    private static IngestionPipeline BuildPipeline(
        IInboundMessageParser? parser = null,
        IPayloadSignatureValidator? validator = null,
        IInboundMessageDeduplicator? deduplicator = null,
        IDistributedEventBus? eventBus = null,
        IDeviceLookup? deviceLookup = null)
    {
        parser ??= DefaultParser();
        validator ??= SubstituteValidator(SignatureValidationResult.Valid);
        deduplicator ??= AcceptingDeduplicator();
        eventBus ??= Substitute.For<IDistributedEventBus>();
        deviceLookup ??= ResolvingDeviceLookup();

        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(Now);

        IMeterFactory meterFactory = new EmptyMeterFactory();

        return new IngestionPipeline(
            parsers: [parser],
            signatureValidators: [validator],
            deviceLookup: deviceLookup,
            deduplicator: deduplicator,
            eventBus: eventBus,
            clock: clock,
            metrics: new IoTMetrics(meterFactory),
            logger: NullLogger<IngestionPipeline>.Instance);
    }

    private static IDeviceLookup ResolvingDeviceLookup()
    {
        IDeviceLookup deviceLookup = Substitute.For<IDeviceLookup>();
        deviceLookup.FindBySerialNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DeviceLookupResult(Guid.NewGuid(), TenantId: null));
        return deviceLookup;
    }
}
