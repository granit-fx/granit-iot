using Granit.IoT.Aws.Jobs.Extensions;
using Granit.IoT.Aws.Shadow;
using Granit.Modularity;

namespace Granit.IoT.Aws.Jobs;

/// <summary>
/// Bridges Granit device commands into AWS IoT Jobs and consumes the
/// <c>DeviceDesiredStateChangedEvent</c> raised by the Shadow bridge so
/// every cloud-issued state change becomes a job dispatched against the
/// matching Thing.
/// </summary>
[DependsOn(typeof(GranitIoTAwsModule))]
[DependsOn(typeof(GranitIoTAwsShadowModule))]
public sealed class GranitIoTAwsJobsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddGranitIoTAwsJobs();
    }
}
