using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Granit.IoT.Aws.Credentials;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Provisioning.Internal;

/// <summary>
/// Real <see cref="IAwsIoTCredentialLoader"/> implementation backed by AWS
/// Secrets Manager. Reads the JSON document
/// <c>{"accessKeyId":..., "secretAccessKey":..., "sessionToken":...}</c>
/// from <see cref="AwsIoTCredentialOptions.FleetCredentialSecretArn"/>.
/// </summary>
internal sealed class AwsSecretsManagerCredentialLoader(
    IAmazonSecretsManager secrets,
    IOptions<AwsIoTCredentialOptions> options)
    : IAwsIoTCredentialLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonSecretsManager _secrets = secrets;
    private readonly AwsIoTCredentialOptions _options = options.Value;

    public async Task<LoadedAwsIoTCredentials?> LoadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.FleetCredentialSecretArn))
        {
            // Signal "use the AWS SDK default credential chain" — same
            // contract as IamRoleAwsIoTCredentialProvider.
            return null;
        }

        GetSecretValueResponse response = await _secrets.GetSecretValueAsync(
            new GetSecretValueRequest { SecretId = _options.FleetCredentialSecretArn },
            cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(response.SecretString))
        {
            throw new InvalidOperationException(
                $"Secrets Manager entry '{_options.FleetCredentialSecretArn}' has no SecretString value.");
        }

        SecretPayload? payload = JsonSerializer.Deserialize<SecretPayload>(response.SecretString, JsonOptions);
        if (payload is null
            || string.IsNullOrEmpty(payload.AccessKeyId)
            || string.IsNullOrEmpty(payload.SecretAccessKey))
        {
            throw new InvalidOperationException(
                $"Secrets Manager entry '{_options.FleetCredentialSecretArn}' is missing required fields " +
                "'accessKeyId' / 'secretAccessKey'.");
        }

        return new LoadedAwsIoTCredentials(
            payload.AccessKeyId,
            payload.SecretAccessKey,
            payload.SessionToken);
    }

    private sealed record SecretPayload(
        [property: JsonPropertyName("accessKeyId")] string AccessKeyId,
        [property: JsonPropertyName("secretAccessKey")] string SecretAccessKey,
        [property: JsonPropertyName("sessionToken")] string? SessionToken);
}
