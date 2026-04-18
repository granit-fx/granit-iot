using Granit.IoT.Aws.Provisioning;
using Granit.IoT.Aws.Provisioning.Internal;
using Granit.IoT.Aws.Provisioning.Options;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Granit.IoT.Aws.Provisioning.Tests.Internal;

public sealed class AwsThingProvisioningOptionsValidatorTests
{
    [Fact]
    public void Validate_NullOptions_Throws()
    {
        AwsThingProvisioningOptionsValidator validator = new();

        Should.Throw<ArgumentNullException>(() => validator.Validate(null, null!));
    }

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        AwsThingProvisioningOptionsValidator validator = new();
        AwsThingProvisioningOptions opts = new()
        {
            DevicePolicyName = "MyPolicy",
            SecretNameTemplate = "iot/devices/{thingName}/key",
        };

        ValidateOptionsResult result = validator.Validate(null, opts);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MissingDevicePolicyName_Fails()
    {
        AwsThingProvisioningOptionsValidator validator = new();
        AwsThingProvisioningOptions opts = new()
        {
            DevicePolicyName = string.Empty,
            SecretNameTemplate = "iot/{thingName}",
        };

        ValidateOptionsResult result = validator.Validate(null, opts);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("DevicePolicyName");
    }

    [Fact]
    public void Validate_TemplateMissingThingNamePlaceholder_Fails()
    {
        AwsThingProvisioningOptionsValidator validator = new();
        AwsThingProvisioningOptions opts = new()
        {
            DevicePolicyName = "MyPolicy",
            SecretNameTemplate = "iot/devices/static",
        };

        ValidateOptionsResult result = validator.Validate(null, opts);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("{thingName}");
    }

    [Fact]
    public void AwsThingProvisioningException_PreservesThingName()
    {
        AwsThingProvisioningException ex = new("thing-1", "msg");

        ex.ThingName.ShouldBe("thing-1");
        ex.Message.ShouldBe("msg");
    }

    [Fact]
    public void AwsThingProvisioningException_WithInner_StoresFields()
    {
        InvalidOperationException inner = new("boom");
        AwsThingProvisioningException ex = new("thing-1", "msg", inner);

        ex.ThingName.ShouldBe("thing-1");
        ex.InnerException.ShouldBeSameAs(inner);
    }
}
