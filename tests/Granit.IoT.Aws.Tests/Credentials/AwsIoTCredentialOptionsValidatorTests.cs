using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Credentials.Internal;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Granit.IoT.Aws.Tests.Credentials;

public sealed class AwsIoTCredentialOptionsValidatorTests
{
    private readonly AwsIoTCredentialOptionsValidator _validator = new();

    [Fact]
    public void NullArn_IsValid()
    {
        var options = new AwsIoTCredentialOptions { FleetCredentialSecretArn = null };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ArnWithCorrectPrefix_IsValid()
    {
        var options = new AwsIoTCredentialOptions
        {
            FleetCredentialSecretArn = "arn:aws:secretsmanager:eu-west-1:123:secret:fleet-AbCdEf",
        };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("not-an-arn")]
    [InlineData("arn:aws:s3:::bucket")]
    public void ArnWithWrongPrefix_IsRejected(string arn)
    {
        var options = new AwsIoTCredentialOptions { FleetCredentialSecretArn = arn };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        result.Failed.ShouldBeTrue();
    }
}
