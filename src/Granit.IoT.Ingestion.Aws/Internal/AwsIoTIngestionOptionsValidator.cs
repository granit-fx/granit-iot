using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Cross-field validator for <see cref="AwsIoTIngestionOptions"/>. Rejects
/// configurations that would start the app in an unsafe or non-functional
/// state: no path enabled, a path enabled without a region, or a non-null
/// <c>Direct.ApiKey</c> outside <c>Development</c> (production secrets must
/// come from Granit.Vault, never appsettings).
/// </summary>
internal sealed class AwsIoTIngestionOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<AwsIoTIngestionOptions>
{
    public ValidateOptionsResult Validate(string? name, AwsIoTIngestionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        bool anyEnabled = options.Sns.Enabled || options.Direct.Enabled || options.ApiGateway.Enabled;
        if (!anyEnabled)
        {
            failures.Add(
                "At least one AWS IoT ingestion path must be enabled (IoT:Ingestion:Aws:Sns:Enabled, " +
                ":Direct:Enabled, or :ApiGateway:Enabled).");
        }

        if (options.Sns.Enabled && string.IsNullOrWhiteSpace(options.Sns.Region))
        {
            failures.Add("IoT:Ingestion:Aws:Sns:Region is required when Sns is enabled.");
        }

        if (options.Direct.Enabled && string.IsNullOrWhiteSpace(options.Direct.Region))
        {
            failures.Add("IoT:Ingestion:Aws:Direct:Region is required when Direct is enabled.");
        }

        if (options.ApiGateway.Enabled && string.IsNullOrWhiteSpace(options.ApiGateway.Region))
        {
            failures.Add("IoT:Ingestion:Aws:ApiGateway:Region is required when ApiGateway is enabled.");
        }

        if (options.Direct.Enabled
            && options.Direct.AuthMode == DirectAuthMode.ApiKey
            && !string.IsNullOrEmpty(options.Direct.ApiKey)
            && !environment.IsDevelopment())
        {
            failures.Add(
                "IoT:Ingestion:Aws:Direct:ApiKey must not be set in appsettings in non-Development environments. " +
                "Load it from Granit.Vault at runtime.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
