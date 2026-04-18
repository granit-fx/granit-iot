using Amazon.IoT;
using Amazon.IoT.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Provisioning.Abstractions;
using Granit.IoT.Aws.Provisioning.Diagnostics;
using Granit.IoT.Aws.Provisioning.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Provisioning.Internal;

internal sealed class ThingProvisioningService(
    IAmazonIoT iot,
    IAmazonSecretsManager secrets,
    IAwsIoTCredentialProvider credentials,
    IOptions<AwsThingProvisioningOptions> options,
    IoTAwsProvisioningMetrics metrics,
    ILogger<ThingProvisioningService> logger)
    : IThingProvisioningService
{
    private readonly IAmazonIoT _iot = iot;
    private readonly IAmazonSecretsManager _secrets = secrets;
    private readonly IAwsIoTCredentialProvider _credentials = credentials;
    private readonly AwsThingProvisioningOptions _options = options.Value;
    private readonly IoTAwsProvisioningMetrics _metrics = metrics;
    private readonly ILogger<ThingProvisioningService> _logger = logger;

    public async Task EnsureThingAsync(AwsThingBinding binding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        EnsureCredentialsReady(binding);
        if (binding.ProvisioningStatus >= AwsThingProvisioningStatus.ThingCreated)
        {
            return;
        }

        string thingName = binding.ThingName.Value;
        string thingArn;
        try
        {
            DescribeThingResponse existing = await _iot.DescribeThingAsync(
                new DescribeThingRequest { ThingName = thingName },
                cancellationToken).ConfigureAwait(false);
            thingArn = existing.ThingArn;
            ProvisioningLog.ThingAlreadyExists(_logger, thingName);
        }
        catch (Amazon.IoT.Model.ResourceNotFoundException)
        {
            CreateThingResponse created = await _iot.CreateThingAsync(
                new CreateThingRequest { ThingName = thingName },
                cancellationToken).ConfigureAwait(false);
            thingArn = created.ThingArn;
            _metrics.RecordThingCreated(binding.TenantId);
            ProvisioningLog.ThingCreated(_logger, thingName);
        }

        binding.RecordThingCreated(thingArn);
    }

    public async Task EnsureCertificateAndSecretAsync(AwsThingBinding binding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        EnsureCredentialsReady(binding);
        if (binding.ProvisioningStatus >= AwsThingProvisioningStatus.SecretStored)
        {
            return;
        }

        // We deliberately couple the cert issuance and the private-key persistence
        // into a single saga step. The private key returned by
        // CreateKeysAndCertificate is *not* recoverable from AWS later — losing
        // it (e.g. crash between AWS call and DB commit) means the cert ARN is
        // useless. Secrets Manager's ClientRequestToken makes the secret-side
        // call idempotent, but the IoT cert call is not. Net effect: an AWS
        // crash between the two calls leaks a dangling certificate; a crash
        // before the DB commit does the same. A reconciliation job (future
        // story) can sweep certificates with no attached principal.

        if (binding.ProvisioningStatus < AwsThingProvisioningStatus.CertIssued)
        {
            CreateKeysAndCertificateResponse keys = await _iot.CreateKeysAndCertificateAsync(
                new CreateKeysAndCertificateRequest { SetAsActive = true },
                cancellationToken).ConfigureAwait(false);

            string secretArn = await StoreSecretAsync(binding, keys.KeyPair.PrivateKey, cancellationToken)
                .ConfigureAwait(false);

            binding.RecordCertificateIssued(keys.CertificateArn);
            binding.RecordSecretStored(secretArn);
            _metrics.RecordCertificateIssued(binding.TenantId);
            ProvisioningLog.CertificateIssued(_logger, binding.ThingName.Value, keys.CertificateId);
        }
        else
        {
            // CertIssued was reached but SecretStored wasn't — by construction
            // (cert + secret are written together) the only way to land here
            // is a dropped-on-the-floor private key. Manual reconciliation
            // required: the Failed state surfaces in the reconciliation query.
            binding.MarkAsFailed(
                "Certificate ARN was persisted but the private key never reached Secrets Manager. " +
                "Operator must revoke the certificate and re-provision the binding.");
            throw new AwsThingProvisioningException(
                binding.ThingName.Value,
                "Cannot recover private key for already-issued certificate; binding marked Failed.");
        }
    }

    public async Task EnsureActivationAsync(AwsThingBinding binding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        EnsureCredentialsReady(binding);
        if (binding.ProvisioningStatus >= AwsThingProvisioningStatus.Active)
        {
            return;
        }

        if (binding.ProvisioningStatus < AwsThingProvisioningStatus.SecretStored
            || string.IsNullOrEmpty(binding.CertificateArn))
        {
            throw new AwsThingProvisioningException(
                binding.ThingName.Value,
                $"Cannot activate binding in state '{binding.ProvisioningStatus}' (CertificateArn missing).");
        }

        // AttachPolicy and AttachThingPrincipal both raise no error when the
        // attachment already exists, so they are safe to replay.
        await _iot.AttachPolicyAsync(
            new AttachPolicyRequest
            {
                PolicyName = _options.DevicePolicyName,
                Target = binding.CertificateArn,
            },
            cancellationToken).ConfigureAwait(false);

        await _iot.AttachThingPrincipalAsync(
            new AttachThingPrincipalRequest
            {
                ThingName = binding.ThingName.Value,
                Principal = binding.CertificateArn,
            },
            cancellationToken).ConfigureAwait(false);

        binding.MarkAsActive();
        _metrics.RecordActivated(binding.TenantId);
        ProvisioningLog.BindingActivated(_logger, binding.ThingName.Value);
    }

    public async Task DecommissionAsync(AwsThingBinding binding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        EnsureCredentialsReady(binding);
        if (binding.ProvisioningStatus is AwsThingProvisioningStatus.Decommissioned)
        {
            return;
        }

        if (!string.IsNullOrEmpty(binding.CertificateArn))
        {
            string certificateId = ExtractCertificateId(binding.CertificateArn);

            // Detach + deactivate first so DeleteCertificate is allowed.
            await TryDetachPrincipalAsync(binding.ThingName.Value, binding.CertificateArn, cancellationToken)
                .ConfigureAwait(false);
            await TryDetachPolicyAsync(binding.CertificateArn, cancellationToken).ConfigureAwait(false);
            await TryUpdateCertificateStatusAsync(certificateId, CertificateStatus.INACTIVE, cancellationToken)
                .ConfigureAwait(false);
            await TryDeleteCertificateAsync(certificateId, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(binding.CertificateSecretArn))
        {
            await TryDeleteSecretAsync(binding.CertificateSecretArn, cancellationToken).ConfigureAwait(false);
        }

        await TryDeleteThingAsync(binding.ThingName.Value, cancellationToken).ConfigureAwait(false);

        binding.MarkAsDecommissioned();
        _metrics.RecordDecommissioned(binding.TenantId);
        ProvisioningLog.BindingDecommissioned(_logger, binding.ThingName.Value);
    }

    private void EnsureCredentialsReady(AwsThingBinding binding)
    {
        if (!_credentials.IsReady)
        {
            throw new AwsThingProvisioningException(
                binding.ThingName.Value,
                "AWS credential provider is not ready; refusing to call AWS.");
        }
    }

    private async Task<string> StoreSecretAsync(
        AwsThingBinding binding,
        string privateKeyPem,
        CancellationToken cancellationToken)
    {
        string secretName = _options.SecretNameTemplate
            .Replace("{thingName}", binding.ThingName.Value, StringComparison.Ordinal);

        try
        {
            CreateSecretResponse response = await _secrets.CreateSecretAsync(
                new CreateSecretRequest
                {
                    Name = secretName,
                    SecretString = privateKeyPem,
                    KmsKeyId = _options.SecretKmsKeyId,
                    // ClientRequestToken makes CreateSecret natively idempotent:
                    // AWS returns the existing ARN when called twice with the
                    // same token (binding.Id is unique per binding).
                    ClientRequestToken = binding.Id.ToString(),
                },
                cancellationToken).ConfigureAwait(false);
            return response.ARN;
        }
        catch (Amazon.SecretsManager.Model.ResourceExistsException)
        {
            // Different ClientRequestToken hit an existing secret name — fetch
            // and reuse rather than failing the saga. This handles the case
            // where a previous run with a different binding.Id created the
            // secret (manual re-provisioning, dev workflow).
            GetSecretValueResponse existing = await _secrets.GetSecretValueAsync(
                new GetSecretValueRequest { SecretId = secretName },
                cancellationToken).ConfigureAwait(false);
            return existing.ARN;
        }
    }

    private static string ExtractCertificateId(string certificateArn)
    {
        // arn:aws:iot:<region>:<account>:cert/<certificateId>
        int slash = certificateArn.LastIndexOf('/');
        return slash < 0 || slash == certificateArn.Length - 1
            ? certificateArn
            : certificateArn[(slash + 1)..];
    }

    private async Task TryDetachPrincipalAsync(string thingName, string certificateArn, CancellationToken cancellationToken)
    {
        try
        {
            await _iot.DetachThingPrincipalAsync(
                new DetachThingPrincipalRequest { ThingName = thingName, Principal = certificateArn },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Amazon.IoT.Model.ResourceNotFoundException)
        {
            // Principal already detached — nothing to do.
        }
    }

    private async Task TryDetachPolicyAsync(string certificateArn, CancellationToken cancellationToken)
    {
        try
        {
            await _iot.DetachPolicyAsync(
                new DetachPolicyRequest { PolicyName = _options.DevicePolicyName, Target = certificateArn },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Amazon.IoT.Model.ResourceNotFoundException)
        {
            // Policy already detached — nothing to do.
        }
    }

    private async Task TryUpdateCertificateStatusAsync(
        string certificateId,
        CertificateStatus status,
        CancellationToken cancellationToken)
    {
        try
        {
            await _iot.UpdateCertificateAsync(
                new UpdateCertificateRequest { CertificateId = certificateId, NewStatus = status },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Amazon.IoT.Model.ResourceNotFoundException)
        {
            // Certificate already gone — nothing to do.
        }
    }

    private async Task TryDeleteCertificateAsync(string certificateId, CancellationToken cancellationToken)
    {
        try
        {
            await _iot.DeleteCertificateAsync(
                new DeleteCertificateRequest { CertificateId = certificateId, ForceDelete = true },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Amazon.IoT.Model.ResourceNotFoundException)
        {
            // Certificate already gone — nothing to do.
        }
    }

    private async Task TryDeleteSecretAsync(string secretArn, CancellationToken cancellationToken)
    {
        try
        {
            await _secrets.DeleteSecretAsync(
                new DeleteSecretRequest { SecretId = secretArn, ForceDeleteWithoutRecovery = true },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Amazon.SecretsManager.Model.ResourceNotFoundException)
        {
            // Secret already gone — nothing to do.
        }
    }

    private async Task TryDeleteThingAsync(string thingName, CancellationToken cancellationToken)
    {
        try
        {
            await _iot.DeleteThingAsync(
                new DeleteThingRequest { ThingName = thingName },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Amazon.IoT.Model.ResourceNotFoundException)
        {
            // Thing already gone — nothing to do.
        }
    }
}
