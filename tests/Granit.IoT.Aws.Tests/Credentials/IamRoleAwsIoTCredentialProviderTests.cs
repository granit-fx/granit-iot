using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Credentials.Internal;
using Shouldly;

namespace Granit.IoT.Aws.Tests.Credentials;

public sealed class IamRoleAwsIoTCredentialProviderTests
{
    [Fact]
    public void DefersToSdkDefaultChain()
    {
        var provider = new IamRoleAwsIoTCredentialProvider();

        provider.AccessKeyId.ShouldBeNull();
        provider.SecretAccessKey.ShouldBeNull();
        provider.SessionToken.ShouldBeNull();
        provider.IsReady.ShouldBeTrue();
    }
}
