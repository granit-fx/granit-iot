using ArchUnitNET.Domain;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Validates conventions for the operational-hardening packages
/// (<c>Granit.IoT.BackgroundJobs</c> + <c>Granit.IoT.Timeline</c>):
/// every <c>IBackgroundJob</c> must be a sealed record decorated with
/// <c>[RecurringJob]</c>, the two packages must remain independent, and
/// the timeline bridge must not depend on the background-jobs package.
/// </summary>
public sealed class BackgroundJobConventionTests
{
    private const string BackgroundJobsPrefix = "Granit.IoT.BackgroundJobs";
    private const string TimelinePrefix = "Granit.IoT.Timeline";
    private const string BackgroundJobInterface = "Granit.BackgroundJobs.IBackgroundJob";
    private const string RecurringJobAttribute = "Granit.BackgroundJobs.RecurringJobAttribute";

    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void IBackgroundJob_implementations_must_be_sealed_records_named_with_Job_suffix()
    {
        Class[] jobs = Architecture.Classes
            .Where(c => c.FullName.StartsWith(BackgroundJobsPrefix + ".", StringComparison.Ordinal))
            .Where(c => c.ImplementedInterfaces.Any(i => i.FullName == BackgroundJobInterface))
            .ToArray();

        jobs.ShouldNotBeEmpty("Granit.IoT.BackgroundJobs must declare at least one IBackgroundJob.");
        jobs.ShouldAllBe(c => c.IsSealed == true);
        jobs.ShouldAllBe(c => c.Name.EndsWith("Job", StringComparison.Ordinal));
    }

    [Fact]
    public void IBackgroundJob_implementations_must_carry_RecurringJob_attribute()
    {
        Class[] jobs = Architecture.Classes
            .Where(c => c.FullName.StartsWith(BackgroundJobsPrefix + ".", StringComparison.Ordinal))
            .Where(c => c.ImplementedInterfaces.Any(i => i.FullName == BackgroundJobInterface))
            .ToArray();

        Class[] missingAttribute = jobs
            .Where(c => !c.Attributes.Any(a => a.FullName == RecurringJobAttribute))
            .ToArray();

        missingAttribute.ShouldBeEmpty(
            "Every IBackgroundJob must declare its cron schedule via [RecurringJob(...)]. Violators: "
            + string.Join(", ", missingAttribute.Select(c => c.FullName)));
    }

    [Fact]
    public void Timeline_package_must_not_depend_on_BackgroundJobs_package()
    {
        Class[] timelineClasses = Architecture.Classes
            .Where(c => c.FullName.StartsWith(TimelinePrefix + ".", StringComparison.Ordinal))
            .ToArray();

        Class[] violators = timelineClasses
            .Where(c => c.Dependencies.Any(d => d.Target.FullName.StartsWith(BackgroundJobsPrefix + ".", StringComparison.Ordinal)))
            .ToArray();

        violators.ShouldBeEmpty(
            "Granit.IoT.Timeline must remain independent from Granit.IoT.BackgroundJobs. Violators: "
            + string.Join(", ", violators.Select(c => c.FullName)));
    }

    [Fact]
    public void BackgroundJobs_package_must_not_depend_on_Timeline_package()
    {
        Class[] backgroundJobsClasses = Architecture.Classes
            .Where(c => c.FullName.StartsWith(BackgroundJobsPrefix + ".", StringComparison.Ordinal))
            .ToArray();

        Class[] violators = backgroundJobsClasses
            .Where(c => c.Dependencies.Any(d => d.Target.FullName.StartsWith(TimelinePrefix + ".", StringComparison.Ordinal)))
            .ToArray();

        violators.ShouldBeEmpty(
            "Granit.IoT.BackgroundJobs must remain independent from Granit.IoT.Timeline. Violators: "
            + string.Join(", ", violators.Select(c => c.FullName)));
    }

    [Fact]
    public void NoOp_partition_maintainer_must_be_internal_sealed()
    {
        Class? noOp = Architecture.Classes
            .FirstOrDefault(c => c.FullName == "Granit.IoT.BackgroundJobs.Internal.NoOpTelemetryPartitionMaintainer");

        noOp.ShouldNotBeNull("NoOpTelemetryPartitionMaintainer must exist as the default fallback.");
        noOp.Visibility.ShouldNotBe(Visibility.Public, "Implementation type must remain internal.");
        noOp.IsSealed.ShouldBe(true);
    }
}
