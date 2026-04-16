# Granit.IoT.Notifications

Bridge between IoT integration events and Granit.Notifications.

Without this package the IoT module emits `TelemetryThresholdExceededEto` but no
user is ever notified. This package subscribes to that event and turns it into an
`IoT.TelemetryThresholdAlert` notification delivered through the channels configured
in Granit.Notifications (Email, Push, SMS).

## What it ships

- `IoTTelemetryThresholdAlertNotificationType` (Warning, Email + Push, no opt-out)
- `IoTDeviceOfflineNotificationType` (Fatal, Email + Push + SMS, no opt-out)
- `TelemetryThresholdNotificationHandler` — Wolverine handler that publishes to
  the followers of the originating `Device` entity
- `IoTNotificationDefinitionProvider` — registers the two notification definitions
- `IoTSettingDefinitionProvider` — registers the per-tenant settings
  (`IoT:TelemetryRetentionDays`, `IoT:IngestRateLimit`,
  `IoT:NotificationThrottleMinutes`, `IoT:Threshold`)

## Anti-flapping

The handler debounces per-`(deviceId, metricName)` via `IConditionalCache` (Redis
SET-NX-PX in production via `Granit.Caching.StackExchangeRedis`, in-memory in dev).
The throttle window is configured per tenant via `IoT:NotificationThrottleMinutes`
(default `15` minutes, clamped to `[1, 1440]`). Without this, a sensor oscillating
around its threshold would generate hundreds of emails per hour per device.

A cache outage fails open: the alert is published anyway, mirroring the policy
of `IdempotencyStoreInboundMessageDeduplicator`.

## Recipient resolution

`PublishToEntityFollowersAsync` is used with `EntityReference("Device", deviceId)`.
Users explicitly following a device receive the alert — no broad tenant-wide
broadcasting.
