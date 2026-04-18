#pragma warning disable CA1873 // Test code: argument allocation is OK
using Granit.IoT.Aws.FleetProvisioning.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Granit.IoT.Aws.FleetProvisioning.Tests.Internal;

public sealed class FleetProvisioningLogTests
{
    [Fact]
    public void AllLoggerMessages_DoNotThrow()
    {
        ILogger logger = NullLogger.Instance;

        Should.NotThrow(() =>
        {
            FleetProvisioningLog.VerifyDeniedDecommissioned(logger, "SN-1");
            FleetProvisioningLog.RegisterCompleted(logger, "SN-1", Guid.NewGuid());
            FleetProvisioningLog.RegisterIdempotent(logger, "SN-1", Guid.NewGuid());
            FleetProvisioningLog.ClaimCertificateExpiring(logger, "thing-1", 14, DateTimeOffset.UtcNow);
            FleetProvisioningLog.RotationTickFailed(logger, new InvalidOperationException("boom"));
        });
    }
}
