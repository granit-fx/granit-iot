using Granit.IoT.Aws.Provisioning.Options;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Provisioning.Internal;

internal sealed class AwsThingProvisioningOptionsValidator
    : IValidateOptions<AwsThingProvisioningOptions>
{
    public ValidateOptionsResult Validate(string? name, AwsThingProvisioningOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.DevicePolicyName))
        {
            failures.Add(
                $"{nameof(AwsThingProvisioningOptions.DevicePolicyName)} is required " +
                "(must be the name of an existing AWS IoT policy).");
        }

        if (!options.SecretNameTemplate.Contains("{thingName}", StringComparison.Ordinal))
        {
            failures.Add(
                $"{nameof(AwsThingProvisioningOptions.SecretNameTemplate)} must contain the literal " +
                "'{thingName}' placeholder so each device gets a unique secret.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
