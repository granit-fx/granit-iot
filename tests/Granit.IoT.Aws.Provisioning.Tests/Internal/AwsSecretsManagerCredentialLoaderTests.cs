using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Provisioning.Internal;
using NSubstitute;
using Shouldly;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Granit.IoT.Aws.Provisioning.Tests.Internal;

public sealed class AwsSecretsManagerCredentialLoaderTests
{
    private readonly IAmazonSecretsManager _secrets = Substitute.For<IAmazonSecretsManager>();

    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenNoFleetArnConfigured()
    {
        AwsSecretsManagerCredentialLoader loader = NewLoader(arn: null);

        LoadedAwsIoTCredentials? result = await loader.LoadAsync(TestContext.Current.CancellationToken);

        result.ShouldBeNull();
        await _secrets.DidNotReceive().GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadAsync_ReturnsCredentialsFromSecretsManager()
    {
        const string Arn = "arn:aws:secretsmanager:eu-west-1:123:secret:fleet";
        _secrets.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse
            {
                ARN = Arn,
                SecretString = """{"accessKeyId":"AKIA-XYZ","secretAccessKey":"sk-XYZ","sessionToken":"tok"}""",
            });

        AwsSecretsManagerCredentialLoader loader = NewLoader(Arn);

        LoadedAwsIoTCredentials? result = await loader.LoadAsync(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.AccessKeyId.ShouldBe("AKIA-XYZ");
        result.SecretAccessKey.ShouldBe("sk-XYZ");
        result.SessionToken.ShouldBe("tok");
    }

    [Fact]
    public async Task LoadAsync_Throws_WhenSecretMissingRequiredFields()
    {
        const string Arn = "arn:aws:secretsmanager:eu-west-1:123:secret:fleet";
        _secrets.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse { ARN = Arn, SecretString = "{}" });

        AwsSecretsManagerCredentialLoader loader = NewLoader(Arn);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            loader.LoadAsync(TestContext.Current.CancellationToken));
    }

    private AwsSecretsManagerCredentialLoader NewLoader(string? arn) =>
        new(_secrets, MsOptions.Create(new AwsIoTCredentialOptions { FleetCredentialSecretArn = arn }));
}
