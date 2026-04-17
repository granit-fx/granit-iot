using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Aws.Credentials;

/// <summary>
/// Configures how the AWS bridge resolves credentials. Two modes:
/// <list type="bullet">
/// <item><see cref="FleetCredentialSecretArn"/> is <c>null</c> →
/// <c>IamRoleAwsIoTCredentialProvider</c> kicks in and the AWS SDK default
/// credential chain (instance role, ECS task role, …) takes over.</item>
/// <item><see cref="FleetCredentialSecretArn"/> is set →
/// <c>RotatingAwsIoTCredentialProvider</c> polls the configured loader every
/// <see cref="RotationCheckIntervalMinutes"/> minutes and serves the latest
/// credential pair through <see cref="IAwsIoTCredentialProvider"/>.</item>
/// </list>
/// </summary>
public sealed class AwsIoTCredentialOptions
{
    /// <summary>Configuration binding section: <c>IoT:Aws:Credentials</c>.</summary>
    public const string SectionName = "IoT:Aws:Credentials";

    /// <summary>
    /// AWS Secrets Manager ARN that holds the fleet credentials JSON
    /// (<c>{"accessKeyId":..., "secretAccessKey":..., "sessionToken":...}</c>).
    /// Leave <c>null</c> to defer to the SDK default credential chain.
    /// </summary>
    public string? FleetCredentialSecretArn { get; set; }

    /// <summary>
    /// How often the rotating provider re-fetches the secret, in minutes.
    /// Defaults to 5 — fast enough that a rotated key starts being used within
    /// the AWS rotation window, slow enough not to hammer Secrets Manager.
    /// </summary>
    [Range(1, 240)]
    public int RotationCheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// How long the rotating provider waits on its initial fetch before
    /// giving up and starting in the not-ready state. Subsequent fetches
    /// happen on the rotation timer.
    /// </summary>
    [Range(1, 60)]
    public int InitialFetchTimeoutSeconds { get; set; } = 15;
}
