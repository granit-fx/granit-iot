using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Amazon.IoT;
using Amazon.IoT.Model;
using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Jobs.Abstractions;
using Granit.IoT.Aws.Jobs.Diagnostics;
using Granit.IoT.Aws.Jobs.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Jobs.Internal;

internal sealed class AwsIoTJobsCommandDispatcher(
    IAmazonIoT iot,
    IJobTrackingStore tracking,
    IAwsIoTCredentialProvider credentials,
    IOptions<AwsIoTJobsOptions> options,
    AwsJobsMetrics metrics,
    ILogger<AwsIoTJobsCommandDispatcher> logger)
    : IDeviceCommandDispatcher
{
    private const string DynamicGroupNamePrefix = "granit-dynamic-";

    private readonly IAmazonIoT _iot = iot;
    private readonly IJobTrackingStore _tracking = tracking;
    private readonly IAwsIoTCredentialProvider _credentials = credentials;
    private readonly AwsIoTJobsOptions _options = options.Value;
    private readonly AwsJobsMetrics _metrics = metrics;
    private readonly ILogger<AwsIoTJobsCommandDispatcher> _logger = logger;

    public string DispatcherName => "AwsIoTJobs";

    public async Task<string> DispatchAsync(
        IDeviceCommand command,
        DeviceCommandTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(target);

        if (!_credentials.IsReady)
        {
            throw new InvalidOperationException(
                "AWS credential provider is not ready; refusing to dispatch IoT Job.");
        }

        string jobId = $"{_options.JobIdPrefix}-{command.CorrelationId}";
        string thingName = ExtractThingName(target);

        // Idempotency fast-path: if we already tracked this correlationId,
        // surface the previously-issued jobId without touching AWS.
        JobTrackingEntry? existing = await _tracking.GetAsync(command.CorrelationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            JobsLog.IdempotentReuse(_logger, command.CorrelationId, existing.JobId);
            _metrics.RecordIdempotentReuse(command.TenantId, command.Operation);
            return existing.JobId;
        }

        string targetArn = await ResolveTargetArnAsync(target, cancellationToken).ConfigureAwait(false);
        string document = JobDocumentBuilder.Build(command);

        try
        {
            await _iot.CreateJobAsync(
                new CreateJobRequest
                {
                    JobId = jobId,
                    Targets = [targetArn],
                    Document = document,
                    TargetSelection = target.Mode == DeviceCommandTargetMode.DynamicThingGroup
                        ? TargetSelection.CONTINUOUS
                        : TargetSelection.SNAPSHOT,
                },
                cancellationToken).ConfigureAwait(false);

            _metrics.RecordDispatched(command.TenantId, command.Operation);
            JobsLog.JobCreated(_logger, jobId, command.Operation);
        }
        catch (Amazon.IoT.Model.ResourceAlreadyExistsException)
        {
            // Same JobId issued twice (lost cache entry, host restart, …) —
            // AWS already has the job; treat it as a successful idempotent
            // re-dispatch and resume tracking.
            _metrics.RecordIdempotentReuse(command.TenantId, command.Operation);
            JobsLog.JobAlreadyExists(_logger, jobId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _metrics.RecordDispatchError(command.TenantId, command.Operation);
            JobsLog.DispatchFailed(_logger, jobId, ex);
            throw;
        }

        await _tracking.SetAsync(
            command.CorrelationId,
            new JobTrackingEntry(
                command.CorrelationId,
                jobId,
                thingName,
                command.TenantId,
                ExpiresAt: default),
            TimeSpan.FromHours(_options.JobTrackingTtlHours),
            cancellationToken).ConfigureAwait(false);

        return jobId;
    }

    private async Task<string> ResolveTargetArnAsync(
        DeviceCommandTarget target,
        CancellationToken cancellationToken)
    {
        return target.Mode switch
        {
            DeviceCommandTargetMode.Thing => target.Value,
            DeviceCommandTargetMode.ThingGroup => target.Value,
            DeviceCommandTargetMode.DynamicThingGroup
                => await EnsureDynamicGroupAsync(target.Value, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException(
                $"Unsupported target mode '{target.Mode}'."),
        };
    }

    private async Task<string> EnsureDynamicGroupAsync(string query, CancellationToken cancellationToken)
    {
        string groupName = DynamicGroupNameFor(query);

        try
        {
            DescribeThingGroupResponse existing = await _iot.DescribeThingGroupAsync(
                new DescribeThingGroupRequest { ThingGroupName = groupName },
                cancellationToken).ConfigureAwait(false);
            return existing.ThingGroupArn;
        }
        catch (Amazon.IoT.Model.ResourceNotFoundException)
        {
            CreateDynamicThingGroupResponse created = await _iot.CreateDynamicThingGroupAsync(
                new CreateDynamicThingGroupRequest
                {
                    ThingGroupName = groupName,
                    QueryString = query,
                },
                cancellationToken).ConfigureAwait(false);
            JobsLog.DynamicGroupCreated(_logger, groupName, query);
            return created.ThingGroupArn;
        }
    }

    private static string ExtractThingName(DeviceCommandTarget target)
    {
        if (target.Mode != DeviceCommandTargetMode.Thing)
        {
            return target.Value;
        }

        // arn:aws:iot:region:account:thing/<thingName>
        int slash = target.Value.LastIndexOf('/');
        return slash < 0 ? target.Value : target.Value[(slash + 1)..];
    }

    private static string DynamicGroupNameFor(string query)
    {
        // Stable, AWS-compliant group name for a given query — the same query
        // always lands in the same group so multiple commands re-use the
        // dynamic group instead of creating one per dispatch.
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(query));
        // 16 hex chars is plenty (2^64 query namespace) and keeps the name
        // well under AWS's 128-char group-name limit.
        var sb = new StringBuilder(DynamicGroupNamePrefix.Length + 16);
        sb.Append(DynamicGroupNamePrefix);
        for (int i = 0; i < 8; i++)
        {
            sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}
