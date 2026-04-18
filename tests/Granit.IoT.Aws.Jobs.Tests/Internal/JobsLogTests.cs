using Granit.IoT.Aws.Jobs.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Granit.IoT.Aws.Jobs.Tests.Internal;

public sealed class JobsLogTests
{
    [Fact]
    public void AllLoggerMessages_DoNotThrow()
    {
        ILogger logger = NullLogger.Instance;

        Should.NotThrow(() =>
        {
            JobsLog.JobCreated(logger, "job-1", "OPERATION");
            JobsLog.JobAlreadyExists(logger, "job-1");
            var correlationId = Guid.NewGuid();
            JobsLog.IdempotentReuse(logger, correlationId, "job-1");
            JobsLog.DispatchFailed(logger, "job-1", new InvalidOperationException("boom"));
            JobsLog.DynamicGroupCreated(logger, "group-1", "query");
            JobsLog.JobCompleted(logger, "job-1", "thing-1");
            JobsLog.JobFailed(logger, "job-1", "thing-1", "FAILED", "boom");
            JobsLog.PollingTickFailed(logger, "job-1", new InvalidOperationException("boom"));
        });
    }
}
