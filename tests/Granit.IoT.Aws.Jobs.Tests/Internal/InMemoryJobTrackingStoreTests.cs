using Granit.IoT.Aws.Jobs.Internal;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Granit.IoT.Aws.Jobs.Tests.Internal;

public sealed class InMemoryJobTrackingStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsEntry()
    {
        var time = new FakeTimeProvider(Now);
        var store = new InMemoryJobTrackingStore(time);
        var correlationId = Guid.NewGuid();
        JobTrackingEntry entry = new(correlationId, "job-1", "thing-1", null, ExpiresAt: default);

        await store.SetAsync(correlationId, entry, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        JobTrackingEntry? retrieved = await store.GetAsync(correlationId, TestContext.Current.CancellationToken);

        retrieved.ShouldNotBeNull();
        retrieved.JobId.ShouldBe("job-1");
        retrieved.ExpiresAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public async Task GetAsync_AfterTtlElapsed_ReturnsNullAndRemoves()
    {
        var time = new FakeTimeProvider(Now);
        var store = new InMemoryJobTrackingStore(time);
        var correlationId = Guid.NewGuid();

        await store.SetAsync(correlationId,
            new JobTrackingEntry(correlationId, "job-1", "thing-1", null, default),
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromMinutes(10));

        (await store.GetAsync(correlationId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task RemoveAsync_DropsEntry()
    {
        var time = new FakeTimeProvider(Now);
        var store = new InMemoryJobTrackingStore(time);
        var correlationId = Guid.NewGuid();
        await store.SetAsync(correlationId,
            new JobTrackingEntry(correlationId, "job-1", "thing-1", null, default),
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken);

        await store.RemoveAsync(correlationId, TestContext.Current.CancellationToken);

        (await store.GetAsync(correlationId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_ReturnsLiveEntriesOnly()
    {
        var time = new FakeTimeProvider(Now);
        var store = new InMemoryJobTrackingStore(time);

        for (int i = 0; i < 3; i++)
        {
            var id = Guid.NewGuid();
            await store.SetAsync(id,
                new JobTrackingEntry(id, $"job-{i}", $"thing-{i}", null, default),
                TimeSpan.FromMinutes(5),
                TestContext.Current.CancellationToken);
        }

        IReadOnlyList<JobTrackingEntry> live = await store.ListAsync(10, TestContext.Current.CancellationToken);

        live.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ListAsync_LimitsResults()
    {
        var time = new FakeTimeProvider(Now);
        var store = new InMemoryJobTrackingStore(time);

        for (int i = 0; i < 5; i++)
        {
            var id = Guid.NewGuid();
            await store.SetAsync(id,
                new JobTrackingEntry(id, $"job-{i}", $"thing-{i}", null, default),
                TimeSpan.FromMinutes(5),
                TestContext.Current.CancellationToken);
        }

        IReadOnlyList<JobTrackingEntry> live = await store.ListAsync(2, TestContext.Current.CancellationToken);

        live.Count.ShouldBe(2);
    }
}
