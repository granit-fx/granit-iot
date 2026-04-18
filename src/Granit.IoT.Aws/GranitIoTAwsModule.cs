using Granit.IoT.Aws.Extensions;
using Granit.Modularity;

namespace Granit.IoT.Aws;

/// <summary>
/// Bridge module: maps cloud-agnostic Granit IoT devices to AWS IoT Core
/// resources via the <c>AwsThingBinding</c> companion aggregate. The actual
/// provisioning workflow (Thing/certificate/secret creation, shadow sync,
/// jobs dispatch, JITP endpoints) lands in companion packages
/// (<c>Granit.IoT.Aws.EntityFrameworkCore</c>, <c>Granit.IoT.Aws.Shadow</c>,
/// <c>Granit.IoT.Aws.Jobs</c>, <c>Granit.IoT.Aws.FleetProvisioning</c>).
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
public sealed class GranitIoTAwsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTAwsCredentials();
    }
}
