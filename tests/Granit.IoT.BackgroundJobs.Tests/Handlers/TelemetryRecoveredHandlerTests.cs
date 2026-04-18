using Granit.IoT.BackgroundJobs.Handlers;
using Granit.IoT.Events;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;

namespace Granit.IoT.BackgroundJobs.Tests.Handlers;

public sealed class TelemetryRecoveredHandlerTests
{
    [Fact]
    public async Task HandleAsync_NullEto_Throws()
    {
        DeviceOfflineTrackerCache tracker = new(new MemoryCache(new MemoryCacheOptions()));

        await Should.ThrowAsync<ArgumentNullException>(() =>
            TelemetryRecoveredHandler.HandleAsync(null!, tracker, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_KnownDevice_RemovesFromTracker()
    {
        DeviceOfflineTrackerCache tracker = new(new MemoryCache(new MemoryCacheOptions()));
        var deviceId = Guid.NewGuid();
        tracker.TryAdd(deviceId, TimeSpan.FromHours(1));

        TelemetryIngestedEto eto = new(
            MessageId: "msg-1",
            DeviceExternalId: "SN-1",
            DeviceId: deviceId,
            TenantId: null,
            RecordedAt: DateTimeOffset.UtcNow,
            Metrics: new Dictionary<string, double> { ["t"] = 1 },
            Source: "test",
            Tags: null);

        await TelemetryRecoveredHandler.HandleAsync(eto, tracker, TestContext.Current.CancellationToken);

        // After Forget, TryAdd should succeed again
        tracker.TryAdd(deviceId, TimeSpan.FromHours(1)).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_UnknownDevice_NoOp()
    {
        DeviceOfflineTrackerCache tracker = new(new MemoryCache(new MemoryCacheOptions()));
        TelemetryIngestedEto eto = new(
            MessageId: "msg-1",
            DeviceExternalId: "SN-1",
            DeviceId: null,
            TenantId: null,
            RecordedAt: DateTimeOffset.UtcNow,
            Metrics: new Dictionary<string, double> { ["t"] = 1 },
            Source: "test",
            Tags: null);

        await TelemetryRecoveredHandler.HandleAsync(eto, tracker, TestContext.Current.CancellationToken);
    }
}
