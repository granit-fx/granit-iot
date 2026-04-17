using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Credentials.Internal;

/// <summary>
/// Cross-field validation for <see cref="AwsIoTCredentialOptions"/>:
/// rejects an obvious typo where the ARN is set to a placeholder string.
/// Range constraints on the timing properties are enforced by data
/// annotations.
/// </summary>
internal sealed class AwsIoTCredentialOptionsValidator : IValidateOptions<AwsIoTCredentialOptions>
{
    private const string ExpectedArnPrefix = "arn:aws:secretsmanager:";

    public ValidateOptionsResult Validate(string? name, AwsIoTCredentialOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.FleetCredentialSecretArn)
            && !options.FleetCredentialSecretArn.StartsWith(ExpectedArnPrefix, StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(AwsIoTCredentialOptions.FleetCredentialSecretArn)} must start with '{ExpectedArnPrefix}' " +
                "(set to null to defer to the SDK default credential chain).");
        }

        return ValidateOptionsResult.Success;
    }
}
