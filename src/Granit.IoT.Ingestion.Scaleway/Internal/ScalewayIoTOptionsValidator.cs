using Granit.IoT.Ingestion.Scaleway.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Ingestion.Scaleway.Internal;

/// <summary>
/// Cross-field validator for <see cref="ScalewayIoTOptions"/>. Rejects a
/// non-empty <see cref="ScalewayIoTOptions.SharedSecret"/> outside the
/// <c>Development</c> environment so production secrets can only land
/// through <c>Granit.Vault</c>, never through appsettings.
/// </summary>
internal sealed class ScalewayIoTOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<ScalewayIoTOptions>
{
    public ValidateOptionsResult Validate(string? name, ScalewayIoTOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.SharedSecret)
            && !environment.IsDevelopment())
        {
            return ValidateOptionsResult.Fail(
                "IoT:Ingestion:Scaleway:SharedSecret must not be set in appsettings in non-Development environments. " +
                "Resolve it from Granit.Vault at runtime and leave the configuration value empty.");
        }

        return ValidateOptionsResult.Success;
    }
}
