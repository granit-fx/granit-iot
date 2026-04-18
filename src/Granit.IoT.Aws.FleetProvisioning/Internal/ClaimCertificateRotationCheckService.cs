using Granit.Events;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.FleetProvisioning.Diagnostics;
using Granit.IoT.Aws.FleetProvisioning.Events;
using Granit.IoT.Aws.FleetProvisioning.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.FleetProvisioning.Internal;

/// <summary>
/// Daily sweep that surfaces every <c>AwsThingBinding</c> whose recorded
/// <c>ClaimCertificateExpiresAt</c> falls inside the configured warning
/// window. Each match is published as <see cref="ClaimCertificateExpiringEvent"/>
/// so consumers (typically <c>Granit.IoT.Notifications</c>) can fan out to
/// the operator on-call channels.
/// </summary>
internal sealed class ClaimCertificateRotationCheckService(
    IServiceScopeFactory scopeFactory,
    IOptions<FleetProvisioningOptions> options,
    IoTAwsFleetProvisioningMetrics metrics,
    ILogger<ClaimCertificateRotationCheckService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly FleetProvisioningOptions _options = options.Value;
    private readonly IoTAwsFleetProvisioningMetrics _metrics = metrics;
    private readonly ILogger<ClaimCertificateRotationCheckService> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = TimeSpan.FromHours(_options.RotationCheckIntervalHours);
        using var timer = new PeriodicTimer(period, _timeProvider);

        try
        {
            await SweepOnceAsync(stoppingToken).ConfigureAwait(false);
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await SweepOnceAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown — expected.
        }
    }

    internal async Task SweepOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            IAwsThingBindingReader reader = scope.ServiceProvider.GetRequiredService<IAwsThingBindingReader>();
            ILocalEventBus bus = scope.ServiceProvider.GetRequiredService<ILocalEventBus>();

            IReadOnlyList<AwsThingBinding> bindings = await reader.ListByStatusAsync(
                [AwsThingProvisioningStatus.Active],
                _options.RotationCheckBatchSize,
                cancellationToken).ConfigureAwait(false);

            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateTimeOffset deadline = now.AddDays(_options.ExpiryWarningWindowDays);

            foreach (AwsThingBinding binding in bindings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (binding.ClaimCertificateExpiresAt is not { } expiresAt
                    || expiresAt > deadline)
                {
                    continue;
                }

                int daysUntilExpiry = Math.Max(0, (int)Math.Ceiling((expiresAt - now).TotalDays));

                FleetProvisioningLog.ClaimCertificateExpiring(
                    _logger, binding.ThingName.Value, daysUntilExpiry, expiresAt);
                _metrics.RecordClaimCertificateExpiring(binding.TenantId);

                await bus.PublishAsync(
                    new ClaimCertificateExpiringEvent(
                        binding.DeviceId,
                        binding.ThingName.Value,
                        expiresAt,
                        daysUntilExpiry,
                        binding.TenantId),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            FleetProvisioningLog.RotationTickFailed(_logger, ex);
        }
    }
}
