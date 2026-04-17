namespace Granit.IoT.Aws.Credentials;

/// <summary>
/// Read-side abstraction over the AWS credentials used by the IoT bridge.
/// AWS client factory lambdas call this provider on every request so a
/// background rotation can swap credentials without restarting the host.
/// </summary>
/// <remarks>
/// Two production-ready implementations ship in <c>Granit.IoT.Aws</c>:
/// <list type="bullet">
/// <item>An IAM-role provider that returns <c>null</c> for both keys so the
/// AWS SDK default credential chain takes over (ECS/EKS task roles).</item>
/// <item>A rotating provider that polls an <see cref="IAwsIoTCredentialLoader"/>
/// at a configurable interval and exposes the latest credentials atomically
/// via <c>volatile</c> reads.</item>
/// </list>
/// </remarks>
public interface IAwsIoTCredentialProvider
{
    /// <summary>
    /// Current AWS access key id, or <c>null</c> if the provider intentionally
    /// defers to the SDK default chain (IAM role).
    /// </summary>
    string? AccessKeyId { get; }

    /// <summary>
    /// Current AWS secret access key, or <c>null</c> if the provider defers to
    /// the SDK default chain.
    /// </summary>
    string? SecretAccessKey { get; }

    /// <summary>
    /// Optional STS session token for temporary credentials. <c>null</c> for
    /// long-lived keys or IAM-role mode (the SDK manages the session token in
    /// the latter case).
    /// </summary>
    string? SessionToken { get; }

    /// <summary>
    /// True once the provider holds a usable credential (or has confirmed it
    /// will defer to the IAM role). AWS endpoints and the host's health check
    /// must gate on this — a request that reaches AWS before <c>IsReady</c>
    /// will fail with an opaque SDK exception.
    /// </summary>
    bool IsReady { get; }
}
