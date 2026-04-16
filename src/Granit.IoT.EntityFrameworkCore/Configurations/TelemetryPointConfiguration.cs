using Granit.IoT.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Granit.IoT.EntityFrameworkCore.Configurations;

internal sealed class TelemetryPointConfiguration : IEntityTypeConfiguration<TelemetryPoint>
{
    public void Configure(EntityTypeBuilder<TelemetryPoint> builder)
    {
        builder.ToTable(
            GranitIoTDbProperties.DbTablePrefix + "telemetry_points",
            GranitIoTDbProperties.DbSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceId)
            .IsRequired();

        builder.Property(x => x.RecordedAt)
            .IsRequired();

        builder.Property(x => x.MessageId)
            .HasMaxLength(128);

        builder.Property(x => x.Source)
            .HasMaxLength(64);

        // Metrics stored as JSON — provider-specific column type (jsonb for PostgreSQL)
        // is applied by the Granit.IoT.EntityFrameworkCore.Postgres extension
        builder.Property(x => x.Metrics)
            .IsRequired();

        // Covering index for time-range queries per device
        builder.HasIndex(x => new { x.DeviceId, x.RecordedAt })
            .IsDescending(false, true)
            .HasDatabaseName($"ix_{GranitIoTDbProperties.DbTablePrefix}telemetry_device_time");

        // GDPR bulk erasure index: per-tenant time-range delete
        builder.HasIndex(x => new { x.TenantId, x.RecordedAt })
            .HasDatabaseName($"ix_{GranitIoTDbProperties.DbTablePrefix}telemetry_tenant_time");
    }
}
