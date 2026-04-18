using Granit.IoT.Aws.Provisioning.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Granit.IoT.Aws.Provisioning.Tests.Internal;

public sealed class ProvisioningLogTests
{
    [Fact]
    public void AllLoggerMessages_DoNotThrow()
    {
        ILogger logger = NullLogger.Instance;

        Should.NotThrow(() =>
        {
            ProvisioningLog.ThingCreated(logger, "thing-1");
            ProvisioningLog.ThingAlreadyExists(logger, "thing-1");
            ProvisioningLog.CertificateIssued(logger, "thing-1", "cert-1");
            ProvisioningLog.BindingActivated(logger, "thing-1");
            ProvisioningLog.BindingDecommissioned(logger, "thing-1");
            ProvisioningLog.ReservationFailed(logger, Guid.NewGuid());
            ProvisioningLog.ProvisioningFailed(logger, "thing-1", new InvalidOperationException("boom"));
        });
    }
}
