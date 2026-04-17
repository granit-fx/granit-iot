using Granit.IoT.Abstractions;

namespace Granit.IoT.BackgroundJobs.Internal;

/// <summary>
/// Default <see cref="ITelemetryPartitionMaintainer"/> registration: reports
/// the parent table as not partitioned, so the maintenance job logs a single
/// warning and exits cleanly. Provider-specific modules (e.g.
/// <c>Granit.IoT.EntityFrameworkCore.Postgres</c>) replace this via
/// <c>RemoveAll</c> + register before resolution.
/// </summary>
internal sealed class NoOpTelemetryPartitionMaintainer : ITelemetryPartitionMaintainer
{
    public Task<bool> IsParentPartitionedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task CreatePartitionAsync(int year, int month, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
