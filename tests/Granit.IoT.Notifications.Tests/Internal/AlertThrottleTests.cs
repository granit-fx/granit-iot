using Granit.Caching;
using Granit.IoT.Notifications;
using Granit.IoT.Notifications.Internal;
using Granit.Settings.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Notifications.Tests.Internal;

public sealed class AlertThrottleTests
{
    private static readonly Guid DeviceId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string MetricName = "temperature";

    [Fact]
    public async Task TryAcquireAsync_CacheAccepts_ReturnsTrue()
    {
        IConditionalCache cache = Substitute.For<IConditionalCache>();
        cache.SetIfAbsentAsync<int>(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);

        AlertThrottle throttle = Build(cache, throttleSetting: "5");

        bool result = await throttle
            .TryAcquireAsync(DeviceId, MetricName, tenantId: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_CacheRejects_ReturnsFalse()
    {
        IConditionalCache cache = Substitute.For<IConditionalCache>();
        cache.SetIfAbsentAsync<int>(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);

        AlertThrottle throttle = Build(cache, throttleSetting: "5");

        bool result = await throttle
            .TryAcquireAsync(DeviceId, MetricName, tenantId: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_CacheThrows_FailsOpen()
    {
        IConditionalCache cache = Substitute.For<IConditionalCache>();
        cache.SetIfAbsentAsync<int>(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new TimeoutException("redis-unavailable"));

        AlertThrottle throttle = Build(cache, throttleSetting: "5");

        bool result = await throttle
            .TryAcquireAsync(DeviceId, MetricName, tenantId: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_UsesConfiguredWindow()
    {
        IConditionalCache cache = Substitute.For<IConditionalCache>();
        cache.SetIfAbsentAsync<int>(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);

        AlertThrottle throttle = Build(cache, throttleSetting: "30");

        await throttle.TryAcquireAsync(DeviceId, MetricName, tenantId: null, TestContext.Current.CancellationToken).ConfigureAwait(true);

        await cache.Received(1)
            .SetIfAbsentAsync<int>(Arg.Any<string>(), Arg.Any<int>(), TimeSpan.FromMinutes(30), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task TryAcquireAsync_MalformedSettingValue_FallsBackToDefault()
    {
        IConditionalCache cache = Substitute.For<IConditionalCache>();
        cache.SetIfAbsentAsync<int>(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);

        AlertThrottle throttle = Build(cache, throttleSetting: "not-a-number");

        await throttle.TryAcquireAsync(DeviceId, MetricName, tenantId: null, TestContext.Current.CancellationToken).ConfigureAwait(true);

        await cache.Received(1)
            .SetIfAbsentAsync<int>(Arg.Any<string>(), Arg.Any<int>(), TimeSpan.FromMinutes(15), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task TryAcquireAsync_KeyFormat_IsStable()
    {
        IConditionalCache cache = Substitute.For<IConditionalCache>();
        cache.SetIfAbsentAsync<int>(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);

        AlertThrottle throttle = Build(cache, throttleSetting: "5");

        await throttle.TryAcquireAsync(DeviceId, MetricName, tenantId: null, TestContext.Current.CancellationToken).ConfigureAwait(true);

        await cache.Received(1)
            .SetIfAbsentAsync<int>(
                Arg.Is<string>(k => k == $"iot-alert:throttle:{DeviceId:N}:{MetricName}"),
                Arg.Any<int>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private static AlertThrottle Build(IConditionalCache cache, string? throttleSetting)
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(IoTSettingNames.NotificationThrottleMinutes, Arg.Any<CancellationToken>())
            .Returns(throttleSetting);

        return new AlertThrottle(cache, settings, NullLogger<AlertThrottle>.Instance);
    }
}
