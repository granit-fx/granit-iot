using System.Diagnostics.Metrics;
using Amazon.IoT;
using Amazon.IoT.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Provisioning;
using Granit.IoT.Aws.Provisioning.Diagnostics;
using Granit.IoT.Aws.Provisioning.Internal;
using Granit.IoT.Aws.Provisioning.Options;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Granit.IoT.Aws.Provisioning.Tests.Internal;

public sealed class ThingProvisioningServiceTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string PolicyName = "TestPolicy";
    private const string ThingArn = "arn:aws:iot:eu-west-1:123:thing/sample";

    // AWS IoT certificate ids are SHA-256 hashes (64 hex chars) — the AWSSDK
    // analyzer (IoT1000) enforces that minimum length on the request models.
    private const string CertId = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
    private const string CertArn = "arn:aws:iot:eu-west-1:123:cert/" + CertId;
    private const string SecretArn = "arn:aws:secretsmanager:eu-west-1:123:secret:iot/devices/x/key-AbC";

    private readonly IAmazonIoT _iot = Substitute.For<IAmazonIoT>();
    private readonly IAmazonSecretsManager _secrets = Substitute.For<IAmazonSecretsManager>();
    private readonly IAwsIoTCredentialProvider _credentials;
    private readonly IoTAwsProvisioningMetrics _metrics;

    public ThingProvisioningServiceTests()
    {
        _credentials = Substitute.For<IAwsIoTCredentialProvider>();
        _credentials.IsReady.Returns(true);

        IMeterFactory meterFactory = new TestMeterFactory();
        _metrics = new IoTAwsProvisioningMetrics(meterFactory);
    }

    [Fact]
    public async Task EnsureThingAsync_CreatesThing_WhenAbsent()
    {
        _iot.DescribeThingAsync(Arg.Any<DescribeThingRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Amazon.IoT.Model.ResourceNotFoundException("nope"));
        _iot.CreateThingAsync(Arg.Any<CreateThingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateThingResponse { ThingArn = ThingArn });

        ThingProvisioningService service = NewService();
        AwsThingBinding binding = NewBinding();

        await service.EnsureThingAsync(binding, TestContext.Current.CancellationToken);

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.ThingCreated);
        binding.ThingArn.ShouldBe(ThingArn);
        await _iot.Received(1).CreateThingAsync(Arg.Any<CreateThingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureThingAsync_NoOp_WhenAlreadyExists()
    {
        _iot.DescribeThingAsync(Arg.Any<DescribeThingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DescribeThingResponse { ThingArn = ThingArn });

        ThingProvisioningService service = NewService();
        AwsThingBinding binding = NewBinding();

        await service.EnsureThingAsync(binding, TestContext.Current.CancellationToken);

        binding.ThingArn.ShouldBe(ThingArn);
        await _iot.DidNotReceive().CreateThingAsync(Arg.Any<CreateThingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureThingAsync_ShortCircuits_WhenAlreadyAdvanced()
    {
        ThingProvisioningService service = NewService();
        AwsThingBinding binding = NewBinding();
        binding.RecordThingCreated(ThingArn);

        await service.EnsureThingAsync(binding, TestContext.Current.CancellationToken);

        await _iot.DidNotReceive().DescribeThingAsync(Arg.Any<DescribeThingRequest>(), Arg.Any<CancellationToken>());
        await _iot.DidNotReceive().CreateThingAsync(Arg.Any<CreateThingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureThingAsync_RefusesWhenCredentialsNotReady()
    {
        _credentials.IsReady.Returns(false);
        ThingProvisioningService service = NewService();

        await Should.ThrowAsync<AwsThingProvisioningException>(() =>
            service.EnsureThingAsync(NewBinding(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EnsureCertificateAndSecretAsync_IssuesCertAndStoresSecret()
    {
        AwsThingBinding binding = NewBinding();
        binding.RecordThingCreated(ThingArn);

        _iot.CreateKeysAndCertificateAsync(Arg.Any<CreateKeysAndCertificateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateKeysAndCertificateResponse
            {
                CertificateArn = CertArn,
                CertificateId = CertId,
                CertificatePem = "-----BEGIN CERTIFICATE-----...",
                KeyPair = new KeyPair { PrivateKey = "PRIVKEY", PublicKey = "PUBKEY" },
            });
        _secrets.CreateSecretAsync(Arg.Any<CreateSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateSecretResponse { ARN = SecretArn });

        ThingProvisioningService service = NewService();

        await service.EnsureCertificateAndSecretAsync(binding, TestContext.Current.CancellationToken);

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.SecretStored);
        binding.CertificateArn.ShouldBe(CertArn);
        binding.CertificateSecretArn.ShouldBe(SecretArn);
        await _secrets.Received(1).CreateSecretAsync(
            Arg.Is<CreateSecretRequest>(r => r.ClientRequestToken == binding.Id.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureCertificateAndSecretAsync_ReusesExistingSecret_WhenNameClashes()
    {
        AwsThingBinding binding = NewBinding();
        binding.RecordThingCreated(ThingArn);

        _iot.CreateKeysAndCertificateAsync(Arg.Any<CreateKeysAndCertificateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateKeysAndCertificateResponse
            {
                CertificateArn = CertArn,
                CertificateId = CertId,
                CertificatePem = "PEM",
                KeyPair = new KeyPair { PrivateKey = "K", PublicKey = "P" },
            });
        _secrets.CreateSecretAsync(Arg.Any<CreateSecretRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Amazon.SecretsManager.Model.ResourceExistsException("name in use"));
        _secrets.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse { ARN = SecretArn });

        ThingProvisioningService service = NewService();

        await service.EnsureCertificateAndSecretAsync(binding, TestContext.Current.CancellationToken);

        binding.CertificateSecretArn.ShouldBe(SecretArn);
    }

    [Fact]
    public async Task EnsureActivationAsync_AttachesPolicyAndPrincipal()
    {
        AwsThingBinding binding = NewBinding();
        binding.RecordThingCreated(ThingArn);
        binding.RecordCertificateIssued(CertArn);
        binding.RecordSecretStored(SecretArn);

        ThingProvisioningService service = NewService();

        await service.EnsureActivationAsync(binding, TestContext.Current.CancellationToken);

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Active);
        await _iot.Received(1).AttachPolicyAsync(
            Arg.Is<AttachPolicyRequest>(r => r.PolicyName == PolicyName && r.Target == CertArn),
            Arg.Any<CancellationToken>());
        await _iot.Received(1).AttachThingPrincipalAsync(
            Arg.Is<AttachThingPrincipalRequest>(r => r.Principal == CertArn),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecommissionAsync_TearsDownAllResources()
    {
        AwsThingBinding binding = NewBinding();
        binding.RecordThingCreated(ThingArn);
        binding.RecordCertificateIssued(CertArn);
        binding.RecordSecretStored(SecretArn);
        binding.MarkAsActive();

        ThingProvisioningService service = NewService();

        await service.DecommissionAsync(binding, TestContext.Current.CancellationToken);

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Decommissioned);
        await _iot.Received(1).DetachThingPrincipalAsync(Arg.Any<DetachThingPrincipalRequest>(), Arg.Any<CancellationToken>());
        await _iot.Received(1).DetachPolicyAsync(Arg.Any<DetachPolicyRequest>(), Arg.Any<CancellationToken>());
        await _iot.Received(1).UpdateCertificateAsync(Arg.Any<UpdateCertificateRequest>(), Arg.Any<CancellationToken>());
        await _iot.Received(1).DeleteCertificateAsync(Arg.Any<DeleteCertificateRequest>(), Arg.Any<CancellationToken>());
        await _iot.Received(1).DeleteThingAsync(Arg.Any<DeleteThingRequest>(), Arg.Any<CancellationToken>());
        await _secrets.Received(1).DeleteSecretAsync(Arg.Any<DeleteSecretRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecommissionAsync_SwallowsResourceNotFoundExceptions()
    {
        AwsThingBinding binding = NewBinding();
        binding.RecordThingCreated(ThingArn);
        binding.RecordCertificateIssued(CertArn);
        binding.RecordSecretStored(SecretArn);
        binding.MarkAsActive();

        _iot.DetachThingPrincipalAsync(Arg.Any<DetachThingPrincipalRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Amazon.IoT.Model.ResourceNotFoundException("gone"));
        _iot.DeleteThingAsync(Arg.Any<DeleteThingRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Amazon.IoT.Model.ResourceNotFoundException("gone"));

        ThingProvisioningService service = NewService();

        await service.DecommissionAsync(binding, TestContext.Current.CancellationToken);

        binding.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Decommissioned);
    }

    private ThingProvisioningService NewService()
    {
        IOptions<AwsThingProvisioningOptions> options = MsOptions.Create(new AwsThingProvisioningOptions
        {
            DevicePolicyName = PolicyName,
            SecretNameTemplate = "iot/devices/{thingName}/key",
        });

        return new ThingProvisioningService(
            _iot,
            _secrets,
            _credentials,
            options,
            _metrics,
            NullLogger<ThingProvisioningService>.Instance);
    }

    private static AwsThingBinding NewBinding()
    {
        var binding = AwsThingBinding.Create(
            Guid.NewGuid(),
            Tenant,
            ThingName.From(Tenant, "SN-001"));
        binding.Id = Guid.NewGuid();
        return binding;
    }

    /// <summary>Lightweight in-memory <see cref="IMeterFactory"/> for tests.</summary>
    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);

        public void Dispose() { }
    }
}
