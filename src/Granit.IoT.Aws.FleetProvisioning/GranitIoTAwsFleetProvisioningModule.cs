using Granit.IoT.Aws.FleetProvisioning.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Aws.FleetProvisioning;

/// <summary>
/// Hosts the AWS IoT Fleet Provisioning (JITP) endpoints and the
/// <c>ClaimCertificateRotationCheckService</c>. The endpoints are mapped
/// via <c>MapGranitIoTAwsFleetProvisioningEndpoints()</c> in the host
/// startup; the rotation check registers as a hosted service automatically.
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitIoTAwsModule))]
public sealed class GranitIoTAwsFleetProvisioningModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTAwsFleetProvisioning();
    }
}
