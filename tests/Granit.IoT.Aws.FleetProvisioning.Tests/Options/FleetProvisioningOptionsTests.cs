using System.ComponentModel.DataAnnotations;
using Granit.IoT.Aws.FleetProvisioning.Events;
using Granit.IoT.Aws.FleetProvisioning.Options;
using Shouldly;

namespace Granit.IoT.Aws.FleetProvisioning.Tests.Options;

public sealed class FleetProvisioningOptionsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        FleetProvisioningOptions opts = new();

        opts.ExpiryWarningWindowDays.ShouldBe(30);
        opts.RotationCheckIntervalHours.ShouldBe(24);
        opts.RotationCheckBatchSize.ShouldBe(1000);
    }

    [Fact]
    public void SectionName_IsStable()
    {
        FleetProvisioningOptions.SectionName.ShouldBe("IoT:Aws:FleetProvisioning");
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(180, true)]
    [InlineData(181, false)]
    public void ExpiryWarningWindowDays_RespectsRange(int value, bool expectedValid)
    {
        FleetProvisioningOptions opts = new() { ExpiryWarningWindowDays = value };
        bool isValid = Validator.TryValidateObject(opts, new ValidationContext(opts), null, validateAllProperties: true);
        isValid.ShouldBe(expectedValid);
    }

    [Fact]
    public void ClaimCertificateExpiringEvent_StoresAllFields()
    {
        var deviceId = Guid.NewGuid();
        var expiresAt = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        ClaimCertificateExpiringEvent evt = new(deviceId, "thing-1", expiresAt, 14, null);

        evt.DeviceId.ShouldBe(deviceId);
        evt.ThingName.ShouldBe("thing-1");
        evt.ExpiresAt.ShouldBe(expiresAt);
        evt.DaysUntilExpiry.ShouldBe(14);
        evt.TenantId.ShouldBeNull();
    }
}
