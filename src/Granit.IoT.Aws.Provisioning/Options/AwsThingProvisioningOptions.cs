using System.ComponentModel.DataAnnotations;

namespace Granit.IoT.Aws.Provisioning.Options;

/// <summary>
/// Configures the AWS Thing provisioning saga: which IoT policy to attach to
/// every device certificate, where to store the private key in Secrets
/// Manager, and a few operational toggles.
/// </summary>
public sealed class AwsThingProvisioningOptions
{
    /// <summary>Configuration binding section: <c>IoT:Aws:Provisioning</c>.</summary>
    public const string SectionName = "IoT:Aws:Provisioning";

    /// <summary>
    /// Name of the AWS IoT policy attached to every freshly issued device
    /// certificate. Must be created out-of-band (Terraform/CloudFormation).
    /// Required when the saga handler is enabled.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string DevicePolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Template used to compose the Secrets Manager secret name for a device's
    /// private key. <c>{thingName}</c> is substituted at runtime. Defaults to
    /// <c>iot/devices/{thingName}/key</c>.
    /// </summary>
    /// <remarks>
    /// The default is a path template, not a credential value — the GRSEC003
    /// suppression below is intentional: the analyzer flags assignments to any
    /// property whose name contains "Secret", but here the value is just the
    /// AWS resource path.
    /// </remarks>
#pragma warning disable GRSEC003
    public string SecretNameTemplate { get; set; } = "iot/devices/{thingName}/key";
#pragma warning restore GRSEC003

    /// <summary>
    /// Optional KMS key id used to encrypt the Secrets Manager secret at rest.
    /// Leave <c>null</c> to use the AWS-managed default key.
    /// </summary>
    public string? SecretKmsKeyId { get; set; }
}
