using Granit.IoT.Ingestion.Aws.Internal;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal;

public class AwsIoTIngestionOptionsValidatorTests
{
    [Fact]
    public void No_path_enabled_fails_validation()
    {
        ValidateOptionsResult result = Validate(new AwsIoTIngestionOptions(), environment: "Production");
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("At least one AWS IoT ingestion path must be enabled");
    }

    [Fact]
    public void Sns_enabled_without_region_fails_validation()
    {
        var options = new AwsIoTIngestionOptions
        {
            Sns = { Enabled = true, Region = "" },
        };
        ValidateOptionsResult result = Validate(options, environment: "Production");
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("Sns:Region is required");
    }

    [Fact]
    public void Direct_enabled_without_region_fails_validation()
    {
        var options = new AwsIoTIngestionOptions
        {
            Direct = { Enabled = true, Region = "" },
        };
        ValidateOptionsResult result = Validate(options, environment: "Production");
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("Direct:Region is required");
    }

    [Fact]
    public void ApiGateway_enabled_without_region_fails_validation()
    {
        var options = new AwsIoTIngestionOptions
        {
            ApiGateway = { Enabled = true, Region = "" },
        };
        ValidateOptionsResult result = Validate(options, environment: "Production");
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("ApiGateway:Region is required");
    }

    [Fact]
    public void Direct_ApiKey_in_non_development_fails_validation()
    {
        var options = new AwsIoTIngestionOptions
        {
            Direct =
            {
                Enabled = true,
                Region = "eu-west-1",
                AuthMode = DirectAuthMode.ApiKey,
                ApiKey = "leaked-key-from-appsettings",
            },
        };
        ValidateOptionsResult result = Validate(options, environment: "Production");
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("ApiKey must not be set in appsettings");
    }

    [Fact]
    public void Direct_ApiKey_in_development_is_allowed()
    {
        var options = new AwsIoTIngestionOptions
        {
            Direct =
            {
                Enabled = true,
                Region = "eu-west-1",
                AuthMode = DirectAuthMode.ApiKey,
                ApiKey = "local-dev-key",
            },
        };
        ValidateOptionsResult result = Validate(options, environment: "Development");
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Direct_SigV4_mode_with_ApiKey_set_is_not_flagged()
    {
        // ApiKey is only validated in ApiKey mode — a leaked value in SigV4 config is noise, not a risk.
        var options = new AwsIoTIngestionOptions
        {
            Direct =
            {
                Enabled = true,
                Region = "eu-west-1",
                AuthMode = DirectAuthMode.SigV4,
                ApiKey = "unused-but-present",
            },
        };
        ValidateOptionsResult result = Validate(options, environment: "Production");
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Fully_valid_Sns_only_config_succeeds()
    {
        var options = new AwsIoTIngestionOptions
        {
            Sns = { Enabled = true, Region = "eu-west-1", TopicArnPrefix = "arn:aws:sns:eu-west-1:1:iot-" },
        };
        ValidateOptionsResult result = Validate(options, environment: "Production");
        result.Succeeded.ShouldBeTrue();
    }

    private static ValidateOptionsResult Validate(AwsIoTIngestionOptions options, string environment)
    {
        IHostEnvironment env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(environment);
        AwsIoTIngestionOptionsValidator validator = new(env);
        return validator.Validate(Microsoft.Extensions.Options.Options.DefaultName, options);
    }
}
