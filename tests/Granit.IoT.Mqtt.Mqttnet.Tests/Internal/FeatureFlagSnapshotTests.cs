using Granit.Features;
using Granit.IoT.Mqtt.Mqttnet.Internal;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Mqtt.Mqttnet.Tests.Internal;

public sealed class FeatureFlagSnapshotTests
{
    private const string Flag = "IoT.MqttBridge";

    [Fact]
    public async Task IsEnabledAsync_FirstCall_HitsChecker()
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        checker.IsEnabledAsync(Flag, Arg.Any<CancellationToken>()).Returns(true);
        FakeTimeProvider clock = new(DateTimeOffset.UnixEpoch);
        FeatureFlagSnapshot snapshot = new(checker, clock, TimeSpan.FromSeconds(30), Flag);

        bool enabled = await snapshot.IsEnabledAsync(TestContext.Current.CancellationToken);

        enabled.ShouldBeTrue();
        await checker.Received(1).IsEnabledAsync(Flag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsEnabledAsync_WithinTtl_DoesNotHitChecker()
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        checker.IsEnabledAsync(Flag, Arg.Any<CancellationToken>()).Returns(true);
        FakeTimeProvider clock = new(DateTimeOffset.UnixEpoch);
        FeatureFlagSnapshot snapshot = new(checker, clock, TimeSpan.FromSeconds(30), Flag);

        await snapshot.IsEnabledAsync(TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(15));
        await snapshot.IsEnabledAsync(TestContext.Current.CancellationToken);

        await checker.Received(1).IsEnabledAsync(Flag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsEnabledAsync_PastTtl_RefreshesAndReflectsFlip()
    {
        IFeatureChecker checker = Substitute.For<IFeatureChecker>();
        int call = 0;
        checker.IsEnabledAsync(Flag, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Interlocked.Increment(ref call) == 1));
        FakeTimeProvider clock = new(DateTimeOffset.UnixEpoch);
        FeatureFlagSnapshot snapshot = new(checker, clock, TimeSpan.FromSeconds(30), Flag);

        bool first = await snapshot.IsEnabledAsync(TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(31));
        bool second = await snapshot.IsEnabledAsync(TestContext.Current.CancellationToken);

        first.ShouldBeTrue();
        second.ShouldBeFalse();
        await checker.Received(2).IsEnabledAsync(Flag, Arg.Any<CancellationToken>());
    }
}
