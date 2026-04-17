using Granit.IoT.Aws.Provisioning.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Aws.Provisioning;

/// <summary>
/// Provisioning bridge module: introduces the AWS SDK and wires the saga
/// handler that maps cloud-agnostic Device events to AWS Thing/Cert/Secret
/// operations. Wolverine discovers <c>AwsThingBridgeHandler</c> via the
/// scanning convention; no extra registration is required beyond depending
/// on this module.
/// </summary>
[DependsOn(typeof(GranitIoTAwsModule))]
public sealed class GranitIoTAwsProvisioningModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTAwsProvisioning();
    }
}
