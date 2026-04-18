using Granit.IoT.Aws.Shadow.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Granit.IoT.Aws.Shadow.Tests.Internal;

public sealed class ShadowLogTests
{
    [Fact]
    public void AllLoggerMessages_DoNotThrow()
    {
        ILogger logger = NullLogger.Instance;

        Should.NotThrow(() =>
        {
            ShadowLog.ReportedPushed(logger, "thing-1");
            ShadowLog.ReportedPushFailed(logger, "thing-1", new InvalidOperationException("boom"));
            ShadowLog.DeltaDetected(logger, "thing-1", 7L, "k1,k2");
            ShadowLog.PollingTickFailed(logger, "thing-1", new InvalidOperationException("boom"));
        });
    }
}
