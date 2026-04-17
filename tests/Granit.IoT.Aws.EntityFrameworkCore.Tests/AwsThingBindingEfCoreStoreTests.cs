using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.EntityFrameworkCore.Internal;
using Granit.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Aws.EntityFrameworkCore.Tests;

public sealed class AwsThingBindingEfCoreStoreTests : IDisposable
{
    private readonly TestDbContextFactory _factory = TestDbContextFactory.Create();
    private readonly AwsThingBindingEfCoreReader _reader;
    private readonly AwsThingBindingEfCoreWriter _writer;

    public AwsThingBindingEfCoreStoreTests()
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        _reader = new AwsThingBindingEfCoreReader(_factory, currentTenant);
        _writer = new AwsThingBindingEfCoreWriter(_factory, currentTenant);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AddAsync_ThenFindByDevice_ReturnsBinding()
    {
        var deviceId = Guid.NewGuid();
        AwsThingBinding binding = NewBinding(deviceId);

        await _writer.AddAsync(binding, TestContext.Current.CancellationToken);

        AwsThingBinding? loaded = await _reader.FindByDeviceAsync(deviceId, TestContext.Current.CancellationToken);
        loaded.ShouldNotBeNull();
        loaded.DeviceId.ShouldBe(deviceId);
        loaded.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Pending);
    }

    [Fact]
    public async Task FindByDeviceAsync_NonExistent_ReturnsNull()
    {
        AwsThingBinding? result = await _reader.FindByDeviceAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindByThingNameAsync_ReturnsMatchingBinding()
    {
        AwsThingBinding binding = NewBinding();
        await _writer.AddAsync(binding, TestContext.Current.CancellationToken);

        AwsThingBinding? loaded = await _reader.FindByThingNameAsync(binding.ThingName, TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(binding.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsSagaProgression()
    {
        AwsThingBinding binding = NewBinding();
        await _writer.AddAsync(binding, TestContext.Current.CancellationToken);

        binding.RecordThingCreated("arn:aws:iot:eu-west-1:123:thing/sample");
        binding.RecordCertificateIssued("arn:aws:iot:eu-west-1:123:cert/abcdef");
        binding.RecordSecretStored("arn:aws:secretsmanager:eu-west-1:123:secret:device-AbCdEf");
        binding.MarkAsActive();
        await _writer.UpdateAsync(binding, TestContext.Current.CancellationToken);

        AwsThingBinding? loaded = await _reader.FindByDeviceAsync(binding.DeviceId, TestContext.Current.CancellationToken);
        loaded.ShouldNotBeNull();
        loaded.ProvisioningStatus.ShouldBe(AwsThingProvisioningStatus.Active);
        loaded.ThingArn.ShouldNotBeNullOrEmpty();
        loaded.CertificateArn.ShouldNotBeNullOrEmpty();
        loaded.CertificateSecretArn.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_BindingNotFoundAfter()
    {
        AwsThingBinding binding = NewBinding();
        await _writer.AddAsync(binding, TestContext.Current.CancellationToken);

        await _writer.DeleteAsync(binding, TestContext.Current.CancellationToken);

        AwsThingBinding? loaded = await _reader.FindByDeviceAsync(binding.DeviceId, TestContext.Current.CancellationToken);
        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task ListByStatusAsync_ReturnsOnlyMatchingStatuses()
    {
        AwsThingBinding pending = NewBinding();
        AwsThingBinding active = NewBinding();
        active.RecordThingCreated("arn:aws:iot:eu-west-1:123:thing/active");
        active.RecordCertificateIssued("arn:aws:iot:eu-west-1:123:cert/active");
        active.RecordSecretStored("arn:aws:secretsmanager:eu-west-1:123:secret:active");
        active.MarkAsActive();

        await _writer.AddAsync(pending, TestContext.Current.CancellationToken);
        await _writer.AddAsync(active, TestContext.Current.CancellationToken);

        IReadOnlyList<AwsThingBinding> result = await _reader.ListByStatusAsync(
            [AwsThingProvisioningStatus.Pending],
            cancellationToken: TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].DeviceId.ShouldBe(pending.DeviceId);
    }

    [Fact]
    public async Task ListByStatusAsync_RespectsBatchSize()
    {
        for (int i = 0; i < 5; i++)
        {
            await _writer.AddAsync(NewBinding(), TestContext.Current.CancellationToken);
        }

        IReadOnlyList<AwsThingBinding> result = await _reader.ListByStatusAsync(
            [AwsThingProvisioningStatus.Pending],
            batchSize: 2,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateThingName()
    {
        var name = ThingName.From(Guid.NewGuid(), "DUP-001");
        var first = AwsThingBinding.Create(Guid.NewGuid(), tenantId: null, name);
        var second = AwsThingBinding.Create(Guid.NewGuid(), tenantId: null, name);

        await _writer.AddAsync(first, TestContext.Current.CancellationToken);

        await Should.ThrowAsync<DbUpdateException>(() =>
            _writer.AddAsync(second, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateDeviceId()
    {
        var deviceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var first = AwsThingBinding.Create(deviceId, tenantId, ThingName.From(tenantId, "FIRST"));
        var second = AwsThingBinding.Create(deviceId, tenantId, ThingName.From(tenantId, "SECOND"));

        await _writer.AddAsync(first, TestContext.Current.CancellationToken);

        await Should.ThrowAsync<DbUpdateException>(() =>
            _writer.AddAsync(second, TestContext.Current.CancellationToken));
    }

    private static AwsThingBinding NewBinding(Guid? deviceId = null)
    {
        Guid actualDeviceId = deviceId ?? Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        return AwsThingBinding.Create(
            actualDeviceId,
            tenantId,
            ThingName.From(tenantId, $"SN-{actualDeviceId.ToString("N")[..8].ToUpperInvariant()}"));
    }
}
