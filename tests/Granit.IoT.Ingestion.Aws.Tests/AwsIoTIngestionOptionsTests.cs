using Granit.IoT.Ingestion.Aws.Options;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests;

public sealed class AwsIoTIngestionOptionsTests
{
    [Fact]
    public void AwsIoTApiGatewayIngestionOptions_Defaults_AreSensible()
    {
        AwsIoTApiGatewayIngestionOptions opts = new();

        opts.Enabled.ShouldBeFalse();
        opts.Region.ShouldBe(string.Empty);
        opts.Stage.ShouldBeNull();
    }

    [Fact]
    public void AwsIoTApiGatewayIngestionOptions_StoresValues()
    {
        AwsIoTApiGatewayIngestionOptions opts = new()
        {
            Enabled = true,
            Region = "eu-west-1",
            Stage = "prod",
        };

        opts.Enabled.ShouldBeTrue();
        opts.Region.ShouldBe("eu-west-1");
        opts.Stage.ShouldBe("prod");
    }

    [Fact]
    public void AwsIoTIngestionOptions_DefaultsExposeNestedSections()
    {
        AwsIoTIngestionOptions opts = new();

        opts.ApiGateway.ShouldNotBeNull();
        opts.Direct.ShouldNotBeNull();
        opts.Sns.ShouldNotBeNull();
    }

    [Fact]
    public void DirectAuthMode_HasExpectedValues()
    {
        ((int)DirectAuthMode.SigV4).ShouldBe(0);
        ((int)DirectAuthMode.ApiKey).ShouldBe(1);
    }
}
