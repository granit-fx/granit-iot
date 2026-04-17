using Amazon.IoT;
using Amazon.IoT.Model;
using Granit.Events;
using Granit.IoT.Aws.Jobs.Diagnostics;
using Granit.IoT.Aws.Jobs.Events;
using Granit.IoT.Aws.Jobs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Jobs.Internal;

/// <summary>
/// Periodically inspects the status of every tracked job and emits
/// <see cref="DeviceCommandCompletedEvent"/> / <see cref="DeviceCommandFailedEvent"/>
/// once the AWS-side execution reaches a terminal state. The tracking
/// entry is removed on terminal status so completed jobs stop being
/// inspected.
/// </summary>
internal sealed class IoTJobStatusPollingService(
    IServiceScopeFactory scopeFactory,
    IOptions<AwsIoTJobsOptions> options,
    AwsJobsMetrics metrics,
    ILogger<IoTJobStatusPollingService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private static readonly HashSet<string> TerminalSuccessStatuses =
        new(StringComparer.Ordinal) { JobStatus.COMPLETED.Value };

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AwsIoTJobsOptions _options = options.Value;
    private readonly AwsJobsMetrics _metrics = metrics;
    private readonly ILogger<IoTJobStatusPollingService> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = TimeSpan.FromSeconds(_options.StatusPollIntervalSeconds);
        using var timer = new PeriodicTimer(period, _timeProvider);

        try
        {
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

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IJobTrackingStore tracking = scope.ServiceProvider.GetRequiredService<IJobTrackingStore>();
        IAmazonIoT iot = scope.ServiceProvider.GetRequiredService<IAmazonIoT>();
        ILocalEventBus bus = scope.ServiceProvider.GetRequiredService<ILocalEventBus>();

        IReadOnlyList<JobTrackingEntry> entries = await tracking
            .ListAsync(_options.StatusPollBatchSize, cancellationToken).ConfigureAwait(false);

        foreach (JobTrackingEntry entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InspectAsync(entry, tracking, iot, bus, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InspectAsync(
        JobTrackingEntry entry,
        IJobTrackingStore tracking,
        IAmazonIoT iot,
        ILocalEventBus bus,
        CancellationToken cancellationToken)
    {
        try
        {
            DescribeJobExecutionResponse response;
            try
            {
                response = await iot.DescribeJobExecutionAsync(
                    new DescribeJobExecutionRequest
                    {
                        JobId = entry.JobId,
                        ThingName = entry.ThingName,
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Amazon.IoT.Model.ResourceNotFoundException)
            {
                // Execution doesn't exist on this Thing yet (job in QUEUED
                // state for a fleet roll-out). Leave it tracked.
                return;
            }

            JobExecution? execution = response.Execution;
            if (execution?.Status is null)
            {
                return;
            }

            string status = execution.Status.Value;
            if (TerminalSuccessStatuses.Contains(status))
            {
                DateTimeOffset completedAt = execution.LastUpdatedAt is { } date
                    ? new DateTimeOffset(date, TimeSpan.Zero)
                    : _timeProvider.GetUtcNow();
                await bus.PublishAsync(
                    new DeviceCommandCompletedEvent(
                        entry.CorrelationId,
                        entry.JobId,
                        entry.ThingName,
                        completedAt,
                        entry.TenantId),
                    cancellationToken).ConfigureAwait(false);
                _metrics.RecordCompleted(entry.TenantId);
                JobsLog.JobCompleted(_logger, entry.JobId, entry.ThingName);
                await tracking.RemoveAsync(entry.CorrelationId, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (IsTerminalFailure(status))
            {
                string? reason = null;
                if (execution.StatusDetails?.DetailsMap is { Count: > 0 } details
                    && details.TryGetValue("detailedStatus", out string? detailed))
                {
                    reason = detailed;
                }

                DateTimeOffset failedAt = execution.LastUpdatedAt is { } date
                    ? new DateTimeOffset(date, TimeSpan.Zero)
                    : _timeProvider.GetUtcNow();
                await bus.PublishAsync(
                    new DeviceCommandFailedEvent(
                        entry.CorrelationId,
                        entry.JobId,
                        entry.ThingName,
                        status,
                        reason,
                        failedAt,
                        entry.TenantId),
                    cancellationToken).ConfigureAwait(false);
                _metrics.RecordFailed(entry.TenantId);
                JobsLog.JobFailed(_logger, entry.JobId, entry.ThingName, status, reason);
                await tracking.RemoveAsync(entry.CorrelationId, cancellationToken).ConfigureAwait(false);
            }

            // Else: status is QUEUED / IN_PROGRESS — keep tracking.
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            JobsLog.PollingTickFailed(_logger, entry.JobId, ex);
        }
    }

    private static bool IsTerminalFailure(string status) =>
        status is "FAILED" or "REJECTED" or "TIMED_OUT" or "CANCELED" or "REMOVED";
}
