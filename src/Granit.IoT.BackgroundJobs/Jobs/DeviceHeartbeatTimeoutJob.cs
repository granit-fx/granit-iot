using Granit.BackgroundJobs;

namespace Granit.IoT.BackgroundJobs.Jobs;

/// <summary>
/// Recurring job that detects offline devices and publishes
/// <c>DeviceOfflineDetectedEto</c> for the notifications module to convert
/// into IoT.DeviceOffline alerts. Runs every 5 minutes; the offline detection
/// is advisory and does not mutate device status.
/// </summary>
[RecurringJob("*/5 * * * *", "iot-heartbeat-timeout")]
public sealed record DeviceHeartbeatTimeoutJob : IBackgroundJob;
