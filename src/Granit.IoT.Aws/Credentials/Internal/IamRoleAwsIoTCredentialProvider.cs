namespace Granit.IoT.Aws.Credentials.Internal;

/// <summary>
/// Default provider when no <c>FleetCredentialSecretArn</c> is configured.
/// Returns <c>null</c> for every credential field so the AWS SDK's default
/// credential chain (instance role, ECS task role, environment variables) is
/// what actually authenticates outbound requests. <see cref="IsReady"/> is
/// always <c>true</c> — there is nothing to fetch.
/// </summary>
internal sealed class IamRoleAwsIoTCredentialProvider : IAwsIoTCredentialProvider
{
    public string? AccessKeyId => null;

    public string? SecretAccessKey => null;

    public string? SessionToken => null;

    public bool IsReady => true;
}
