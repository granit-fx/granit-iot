using Granit.Events;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Shadow.Abstractions;
using Granit.IoT.Aws.Shadow.Diagnostics;
using Granit.IoT.Aws.Shadow.Events;
using Granit.IoT.Aws.Shadow.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Shadow.Internal;

/// <summary>
/// Background sweeper that polls Active <see cref="AwsThingBinding"/>s for
/// a non-empty Device Shadow delta and emits
/// <see cref="DeviceDesiredStateChangedEvent"/> via Wolverine when one is
/// found. Production deployments with high device counts should switch to
/// the event-driven path (IoT Rule on <c>$aws/things/+/shadow/update</c>
/// fanned out via SNS/SQS). The polling service exists for the MVP path
/// and for environments where the event-driven plumbing is not desirable.
/// </summary>
internal sealed class ShadowDeltaPollingService(
    IServiceScopeFactory scopeFactory,
    IOptions<AwsShadowOptions> options,
    IoTAwsShadowMetrics metrics,
    ILogger<ShadowDeltaPollingService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AwsShadowOptions _options = options.Value;
    private readonly IoTAwsShadowMetrics _metrics = metrics;
    private readonly ILogger<ShadowDeltaPollingService> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = TimeSpan.FromSeconds(_options.PollIntervalSeconds);
        using var timer = new PeriodicTimer(period, _timeProvider);

        try
        {
            // Run an immediate pass on startup so a scheduled rollout sees
            // pending deltas without waiting for the first interval.
            await PollOnceAsync(stoppingToken).ConfigureAwait(false);
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await PollOnceAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown — expected.
        }
    }

    internal async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IAwsThingBindingReader bindings = scope.ServiceProvider.GetRequiredService<IAwsThingBindingReader>();
        IDeviceShadowSyncService shadow = scope.ServiceProvider.GetRequiredService<IDeviceShadowSyncService>();
        ILocalEventBus bus = scope.ServiceProvider.GetRequiredService<ILocalEventBus>();

        IReadOnlyList<AwsThingBinding> active = await bindings
            .ListByStatusAsync([AwsThingProvisioningStatus.Active], _options.PollBatchSize, cancellationToken)
            .ConfigureAwait(false);

        foreach (AwsThingBinding binding in active)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessBindingAsync(binding, shadow, bus, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessBindingAsync(
        AwsThingBinding binding,
        IDeviceShadowSyncService shadow,
        ILocalEventBus bus,
        CancellationToken cancellationToken)
    {
        try
        {
            DeviceShadowSnapshot? snapshot = await shadow.GetShadowAsync(binding.ThingName, cancellationToken)
                .ConfigureAwait(false);
            if (snapshot is null || snapshot.Delta.Count == 0)
            {
                return;
            }

            _metrics.RecordDeltaDetected(binding.TenantId);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                string keys = string.Join(", ", snapshot.Delta.Keys);
                ShadowLog.DeltaDetected(_logger, binding.ThingName.Value, snapshot.Version, keys);
            }

            var evt = new DeviceDesiredStateChangedEvent(
                binding.DeviceId,
                binding.ThingName.Value,
                snapshot.Delta,
                snapshot.Version,
                binding.TenantId);

            await bus.PublishAsync(evt, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One bad binding must not stop the sweep — record + move on.
            ShadowLog.PollingTickFailed(_logger, binding.ThingName.Value, ex);
        }
    }
}
