using Granit.IoT.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Granit.IoT.EntityFrameworkCore.Configurations;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable(
            GranitIoTDbProperties.DbTablePrefix + "devices",
            GranitIoTDbProperties.DbSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SerialNumber)
            .HasMaxLength(DeviceSerialNumber.MaxLength)
            .HasConversion(v => v.Value, v => DeviceSerialNumber.Create(v))
            .IsRequired();

        builder.Property(x => x.Model)
            .HasMaxLength(HardwareModel.MaxLength)
            .HasConversion(v => v.Value, v => HardwareModel.Create(v))
            .IsRequired();

        builder.Property(x => x.Firmware)
            .HasMaxLength(FirmwareVersion.MaxLength)
            .HasConversion(v => v.Value, v => FirmwareVersion.Create(v))
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Label)
            .HasMaxLength(256);

        builder.Property(x => x.SuspensionReason)
            .HasMaxLength(1024);

        builder.OwnsOne(x => x.Credential, credential =>
        {
            credential.Property(c => c.CredentialType)
                .HasMaxLength(DeviceCredential.MaxCredentialTypeLength)
                .IsRequired();

            credential.Property(c => c.ProtectedSecret)
                .HasMaxLength(DeviceCredential.MaxProtectedSecretLength)
                .IsRequired();
        });

        // Tags stored as JSON — provider-specific column type (jsonb for PostgreSQL)
        // is applied by the Granit.IoT.EntityFrameworkCore.Postgres extension
        builder.Property(x => x.Tags);

        // Unique serial number per tenant
        builder.HasIndex(x => new { x.TenantId, x.SerialNumber })
            .IsUnique()
            .HasDatabaseName($"ix_{GranitIoTDbProperties.DbTablePrefix}devices_tenant_serial");

        // Filter by status per tenant
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName($"ix_{GranitIoTDbProperties.DbTablePrefix}devices_tenant_status");
    }
}
