using Granit.IoT.BackgroundJobs.Internal;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;

namespace Granit.IoT.BackgroundJobs.Tests.Internal;

public sealed class DeviceOfflineTrackerCacheTests
{
    [Fact]
    public void TryAdd_FirstCall_ReturnsTrue()
    {
        DeviceOfflineTrackerCache tracker = CreateTracker();

        tracker.TryAdd(Guid.NewGuid(), TimeSpan.FromMinutes(60)).ShouldBeTrue();
    }

    [Fact]
    public void TryAdd_SecondCallSameDevice_ReturnsFalse()
    {
        DeviceOfflineTrackerCache tracker = CreateTracker();
        var id = Guid.NewGuid();

        tracker.TryAdd(id, TimeSpan.FromMinutes(60)).ShouldBeTrue();
        tracker.TryAdd(id, TimeSpan.FromMinutes(60)).ShouldBeFalse();
    }

    [Fact]
    public void Forget_ResetsEntry_AndDeviceBecomesAlertableAgain()
    {
        DeviceOfflineTrackerCache tracker = CreateTracker();
        var id = Guid.NewGuid();

        tracker.TryAdd(id, TimeSpan.FromMinutes(60)).ShouldBeTrue();
        tracker.Forget(id);
        tracker.TryAdd(id, TimeSpan.FromMinutes(60)).ShouldBeTrue();
    }

    [Fact]
    public void TryAdd_DifferentDevices_EachReturnsTrueOnce()
    {
        DeviceOfflineTrackerCache tracker = CreateTracker();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        tracker.TryAdd(a, TimeSpan.FromMinutes(60)).ShouldBeTrue();
        tracker.TryAdd(b, TimeSpan.FromMinutes(60)).ShouldBeTrue();
        tracker.TryAdd(a, TimeSpan.FromMinutes(60)).ShouldBeFalse();
    }

    private static DeviceOfflineTrackerCache CreateTracker() =>
        new(new MemoryCache(new MemoryCacheOptions()));
}
