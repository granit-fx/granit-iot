using Granit.IoT.Aws.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Granit.IoT.Aws.EntityFrameworkCore.Configurations;

internal sealed class AwsThingBindingConfiguration : IEntityTypeConfiguration<AwsThingBinding>
{
    public const int ThingArnMaxLength = 256;
    public const int CertificateArnMaxLength = 256;
    public const int CertificateSecretArnMaxLength = 2048;
    public const int FailureReasonMaxLength = 1024;

    public void Configure(EntityTypeBuilder<AwsThingBinding> builder)
    {
        builder.ToTable(
            GranitIoTAwsDbProperties.DbTablePrefix + "thing_bindings",
            GranitIoTAwsDbProperties.DbSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceId)
            .IsRequired();

        builder.Property(x => x.ThingName)
            .HasMaxLength(ThingName.MaxLength)
            .HasConversion(v => v.Value, v => ThingName.Create(v))
            .IsRequired();

        builder.Property(x => x.ThingArn)
            .HasMaxLength(ThingArnMaxLength);

        builder.Property(x => x.CertificateArn)
            .HasMaxLength(CertificateArnMaxLength);

        builder.Property(x => x.CertificateSecretArn)
            .HasMaxLength(CertificateSecretArnMaxLength);

        builder.Property(x => x.ProvisioningStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(FailureReasonMaxLength);

        builder.Property(x => x.ProvisionedViaJitp)
            .IsRequired();

        // 1:1 with Device — enforced at the database level.
        builder.HasIndex(x => new { x.TenantId, x.DeviceId })
            .IsUnique()
            .HasDatabaseName($"ix_{GranitIoTAwsDbProperties.DbTablePrefix}thing_bindings_tenant_device");

        // ThingName is global on the AWS account; uniqueness must be global, not per tenant.
        // Bridge handlers compose ThingName from {tenantId:N}-{serial} so the global unique
        // constraint also enforces tenant isolation at the database level.
        builder.HasIndex(x => x.ThingName)
            .IsUnique()
            .HasDatabaseName($"ix_{GranitIoTAwsDbProperties.DbTablePrefix}thing_bindings_thing_name");

        // Reconciliation queries surface stuck (Pending) or expired (ClaimCertificateExpiresAt) bindings.
        builder.HasIndex(x => new { x.TenantId, x.ProvisioningStatus })
            .HasDatabaseName($"ix_{GranitIoTAwsDbProperties.DbTablePrefix}thing_bindings_tenant_status");
    }
}
