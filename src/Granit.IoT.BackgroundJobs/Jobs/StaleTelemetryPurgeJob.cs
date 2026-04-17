using Granit.BackgroundJobs;

namespace Granit.IoT.BackgroundJobs.Jobs;

/// <summary>
/// Recurring job that enforces per-tenant telemetry retention (GDPR right to
/// erasure). Runs nightly at 03:00 UTC.
/// </summary>
[RecurringJob("0 3 * * *", "iot-stale-telemetry-purge")]
public sealed record StaleTelemetryPurgeJob : IBackgroundJob;
