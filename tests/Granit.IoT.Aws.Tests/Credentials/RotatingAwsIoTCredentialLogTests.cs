using Granit.IoT.Aws.Credentials.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Granit.IoT.Aws.Tests.Credentials;

public sealed class RotatingAwsIoTCredentialLogTests
{
    [Fact]
    public void AllLoggerMessages_DoNotThrow()
    {
        ILogger logger = NullLogger.Instance;

        Should.NotThrow(() =>
        {
            RotatingAwsIoTCredentialLog.CredentialsLoaded(logger, "AKIAEXAMPLE");
            RotatingAwsIoTCredentialLog.RefreshFailed(logger, new InvalidOperationException("boom"));
            RotatingAwsIoTCredentialLog.InitialFetchTimedOut(logger, 30);
            RotatingAwsIoTCredentialLog.RotationDetected(logger, "AKIANEW");
        });
    }
}
