using Granit.IoT.Events;
using Granit.IoT.Wolverine.Internal;
using Granit.Settings.Services;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Wolverine;

public sealed class SettingsDeviceThresholdEvaluatorTests
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset RecordedAt = new(2026, 4, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EvaluateAsync_NoThresholdConfigured_ReturnsEmpty()
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        SettingsDeviceThresholdEvaluator evaluator = new(settings);

        IReadOnlyList<TelemetryThresholdExceededEto> breaches = await evaluator
            .EvaluateAsync(DeviceId, TenantId, new Dictionary<string, double> { ["temp"] = 99.0 }, RecordedAt, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        breaches.ShouldBeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_MetricBelowThreshold_ReturnsEmpty()
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync("IoT:Threshold:temp", Arg.Any<CancellationToken>())
            .Returns("100.0");

        SettingsDeviceThresholdEvaluator evaluator = new(settings);

        IReadOnlyList<TelemetryThresholdExceededEto> breaches = await evaluator
            .EvaluateAsync(DeviceId, TenantId, new Dictionary<string, double> { ["temp"] = 50.0 }, RecordedAt, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        breaches.ShouldBeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_MetricAboveThreshold_ReturnsBreach()
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync("IoT:Threshold:temp", Arg.Any<CancellationToken>())
            .Returns("25.0");

        SettingsDeviceThresholdEvaluator evaluator = new(settings);

        IReadOnlyList<TelemetryThresholdExceededEto> breaches = await evaluator
            .EvaluateAsync(DeviceId, TenantId, new Dictionary<string, double> { ["temp"] = 28.5 }, RecordedAt, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        breaches.Count.ShouldBe(1);
        breaches[0].MetricName.ShouldBe("temp");
        breaches[0].ObservedValue.ShouldBe(28.5);
        breaches[0].ThresholdValue.ShouldBe(25.0);
        breaches[0].DeviceId.ShouldBe(DeviceId);
        breaches[0].TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task EvaluateAsync_NonNumericThreshold_IsIgnored()
    {
        ISettingProvider settings = Substitute.For<ISettingProvider>();
        settings.GetOrNullAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("not-a-number");

        SettingsDeviceThresholdEvaluator evaluator = new(settings);

        IReadOnlyList<TelemetryThresholdExceededEto> breaches = await evaluator
            .EvaluateAsync(DeviceId, TenantId, new Dictionary<string, double> { ["temp"] = 999.0 }, RecordedAt, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        breaches.ShouldBeEmpty();
    }
}
