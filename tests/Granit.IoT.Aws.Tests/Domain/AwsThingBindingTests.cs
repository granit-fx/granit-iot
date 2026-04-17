using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Events;
using Shouldly;

namespace Granit.IoT.Aws.Tests.Domain;

public sealed class AwsThingBindingTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string ThingArn = "arn:aws:iot:eu-west-1:123:thing/sample";
    private const string CertArn = "arn:aws:iot:eu-west-1:123:cert/abcdef";
    private const string SecretArn = "arn:aws:secretsmanager:eu-west-1:123:secret:device/key-AbCdEf";

    private static AwsThingBinding NewPending()
    {
        var binding = AwsThingBinding.Create(
            Guid.NewGuid(),
            Tenant,
            ThingName.From(Tenant, "SN-001"));
        binding.Id = Guid.NewGuid();
        return binding;
    }

    [Fact]
    public void Create_StartsInPending()
    {
        AwsThingBinding binding = NewPending();

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Pending);
        binding.ThingArn.ShouldBeNull();
        binding.CertificateArn.ShouldBeNull();
        binding.CertificateSecretArn.ShouldBeNull();
        binding.ProvisionedViaJitp.ShouldBeFalse();
    }

    [Fact]
    public void Create_RejectsEmptyDeviceId()
    {
        Should.Throw<ArgumentException>(() =>
            AwsThingBinding.Create(Guid.Empty, Tenant, ThingName.From(Tenant, "SN-001")));
    }

    [Fact]
    public void SagaWalkthrough_ReachesActiveAndRaisesProvisionedEvent()
    {
        AwsThingBinding binding = NewPending();

        binding.RecordThingCreated(ThingArn);
        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.ThingCreated);

        binding.RecordCertificateIssued(CertArn);
        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.CertIssued);

        binding.RecordSecretStored(SecretArn);
        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.SecretStored);

        binding.MarkAsActive();

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Active);
        binding.DomainEvents.ShouldContain(e => e is AwsThingProvisionedEvent);
    }

    [Fact]
    public void RecordThingCreated_IsIdempotent()
    {
        AwsThingBinding binding = NewPending();
        binding.RecordThingCreated(ThingArn);

        binding.RecordThingCreated(ThingArn);

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.ThingCreated);
        binding.ThingArn.ShouldBe(ThingArn);
    }

    [Fact]
    public void RecordCertificateIssued_RequiresThingCreated()
    {
        AwsThingBinding binding = NewPending();

        Should.Throw<InvalidOperationException>(() => binding.RecordCertificateIssued(CertArn));
    }

    [Fact]
    public void RecordSecretStored_RequiresCertIssued()
    {
        AwsThingBinding binding = NewPending();
        binding.RecordThingCreated(ThingArn);

        Should.Throw<InvalidOperationException>(() => binding.RecordSecretStored(SecretArn));
    }

    [Fact]
    public void MarkAsActive_RequiresSecretStored()
    {
        AwsThingBinding binding = NewPending();
        binding.RecordThingCreated(ThingArn);
        binding.RecordCertificateIssued(CertArn);

        Should.Throw<InvalidOperationException>(() => binding.MarkAsActive());
    }

    [Fact]
    public void MarkAsActive_IsIdempotent()
    {
        AwsThingBinding binding = NewPending();
        binding.RecordThingCreated(ThingArn);
        binding.RecordCertificateIssued(CertArn);
        binding.RecordSecretStored(SecretArn);
        binding.MarkAsActive();
        binding.ClearDomainEvents();

        binding.MarkAsActive();

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Active);
        binding.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkAsDecommissioned_RaisesEvent()
    {
        AwsThingBinding binding = NewPending();
        binding.RecordThingCreated(ThingArn);
        binding.RecordCertificateIssued(CertArn);
        binding.RecordSecretStored(SecretArn);
        binding.MarkAsActive();

        binding.MarkAsDecommissioned();

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Decommissioned);
        binding.DomainEvents.ShouldContain(e => e is AwsThingDecommissionedEvent);
    }

    [Fact]
    public void RecordThingCreated_AfterDecommission_Throws()
    {
        AwsThingBinding binding = NewPending();
        binding.MarkAsDecommissioned();

        Should.Throw<InvalidOperationException>(() => binding.RecordThingCreated(ThingArn));
    }

    [Fact]
    public void MarkAsFailed_CapturesReason()
    {
        AwsThingBinding binding = NewPending();
        binding.RecordThingCreated(ThingArn);

        binding.MarkAsFailed("Certificate quota exceeded");

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Failed);
        binding.FailureReason.ShouldBe("Certificate quota exceeded");
    }

    [Fact]
    public void CreateForJitp_LandsInActiveAndRaisesProvisionedEvent()
    {
        var deviceId = Guid.NewGuid();

        var binding = AwsThingBinding.CreateForJitp(
            deviceId,
            Tenant,
            ThingName.From(Tenant, "SN-JITP-001"),
            ThingArn,
            CertArn,
            SecretArn);

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Active);
        binding.ProvisionedViaJitp.ShouldBeTrue();
        binding.ThingArn.ShouldBe(ThingArn);
        binding.CertificateArn.ShouldBe(CertArn);
        binding.CertificateSecretArn.ShouldBe(SecretArn);
        binding.DomainEvents.ShouldContain(e => e is AwsThingProvisionedEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateForJitp_RequiresArns(string? blank)
    {
        Should.Throw<ArgumentException>(() => AwsThingBinding.CreateForJitp(
            Guid.NewGuid(),
            Tenant,
            ThingName.From(Tenant, "SN-001"),
            blank!,
            CertArn,
            SecretArn));
    }

    [Fact]
    public void RecordShadowReportedAt_PersistsTimestamp()
    {
        AwsThingBinding binding = NewPending();
        DateTimeOffset at = DateTimeOffset.UtcNow;

        binding.RecordShadowReportedAt(at);

        binding.LastShadowReportedAt.ShouldBe(at);
    }
}
