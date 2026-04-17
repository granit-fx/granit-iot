using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Credentials.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Extensions;

public static class AwsCredentialServiceCollectionExtensions
{
    /// <summary>
    /// Wires the AWS bridge credential pipeline. The concrete
    /// <see cref="IAwsIoTCredentialProvider"/> registered depends on the bound
    /// configuration — when <see cref="AwsIoTCredentialOptions.FleetCredentialSecretArn"/>
    /// is null we register the IAM-role provider, otherwise the rotating
    /// provider plus the matching <c>IHostedService</c>. The
    /// <see cref="IAwsIoTCredentialLoader"/> implementation must be registered
    /// separately by the host (e.g. an AWS Secrets Manager loader from the
    /// PR #4 / story #47 follow-up).
    /// </summary>
    public static IServiceCollection AddGranitIoTAwsCredentials(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AwsIoTCredentialOptions>()
            .BindConfiguration(AwsIoTCredentialOptions.SectionName)
            .ValidateDataAnnotations();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AwsIoTCredentialOptions>, AwsIoTCredentialOptionsValidator>());

        services.TryAddSingleton(TimeProvider.System);

        // Resolve the provider lazily so the post-configuration ARN (set by a
        // host that binds the section after AddGranitIoTAwsCredentials)
        // is honoured.
        services.TryAddSingleton<IAwsIoTCredentialProvider>(sp =>
        {
            AwsIoTCredentialOptions options = sp.GetRequiredService<IOptions<AwsIoTCredentialOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.FleetCredentialSecretArn))
            {
                return new IamRoleAwsIoTCredentialProvider();
            }

            return sp.GetRequiredService<RotatingAwsIoTCredentialProvider>();
        });

        services.TryAddSingleton<RotatingAwsIoTCredentialProvider>();
        services.AddHostedService(sp => sp.GetRequiredService<RotatingAwsIoTCredentialProvider>());

        return services;
    }
}
