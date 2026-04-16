#pragma warning disable CA2012 // NSubstitute ValueTask setup — consumed exactly once per call
using Granit.Http.Idempotency.Abstractions;
using Granit.Http.Idempotency.Models;
using Granit.IoT.Ingestion.Internal;
using Granit.IoT.Ingestion.Options;
using Granit.Timing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Pipeline;

public sealed class IdempotencyStoreInboundMessageDeduplicatorTests
{
    [Fact]
    public async Task TryAcquireAsync_StoreAccepts_ReturnsTrue()
    {
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();
        store.TryAcquireAsync(
                Arg.Is<string>(s => s == "iot-msg:msg-1"),
                Arg.Any<IdempotencyEntry>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        IdempotencyStoreInboundMessageDeduplicator dedup = Build(store);

        bool result = await dedup
            .TryAcquireAsync("msg-1", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_StoreRejects_ReturnsFalse()
    {
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();
        store.TryAcquireAsync(
                Arg.Any<string>(),
                Arg.Any<IdempotencyEntry>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        IdempotencyStoreInboundMessageDeduplicator dedup = Build(store);

        bool result = await dedup
            .TryAcquireAsync("msg-1", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_StoreThrows_FailsOpen()
    {
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();
        store.TryAcquireAsync(
                Arg.Any<string>(),
                Arg.Any<IdempotencyEntry>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new TimeoutException("redis-unavailable"));

        IdempotencyStoreInboundMessageDeduplicator dedup = Build(store);

        bool result = await dedup
            .TryAcquireAsync("msg-1", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.ShouldBeTrue();
    }

    private static IdempotencyStoreInboundMessageDeduplicator Build(IIdempotencyStore store)
    {
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(DateTimeOffset.UnixEpoch);

        IOptions<GranitIoTIngestionOptions> options = Microsoft.Extensions.Options.Options.Create(new GranitIoTIngestionOptions
        {
            DeduplicationWindowMinutes = 5,
        });

        return new IdempotencyStoreInboundMessageDeduplicator(
            store,
            clock,
            options,
            NullLogger<IdempotencyStoreInboundMessageDeduplicator>.Instance);
    }
}
