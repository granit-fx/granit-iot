namespace Granit.IoT.Aws.Credentials;

/// <summary>
/// Snapshot of credentials returned by <see cref="IAwsIoTCredentialLoader"/>.
/// The rotating provider stores these in <c>volatile</c> fields and exposes
/// them through <see cref="IAwsIoTCredentialProvider"/> so consumers always
/// see a consistent triplet.
/// </summary>
public sealed record LoadedAwsIoTCredentials(
    string AccessKeyId,
    string SecretAccessKey,
    string? SessionToken = null);
