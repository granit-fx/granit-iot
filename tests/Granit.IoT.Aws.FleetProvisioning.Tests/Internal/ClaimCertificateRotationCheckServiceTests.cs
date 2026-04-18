using System.Diagnostics.Metrics;
using Granit.Events;
using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.FleetProvisioning.Diagnostics;
using Granit.IoT.Aws.FleetProvisioning.Events;
using Granit.IoT.Aws.FleetProvisioning.Internal;
using Granit.IoT.Aws.FleetProvisioning.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Aws.FleetProvisioning.Tests.Internal;

public sealed class ClaimCertificateRotationCheckServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SweepOnceAsync_NoBindings_PublishesNothing()
    {
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AwsThingBinding>());
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ClaimCertificateRotationCheckService service = NewService(reader, bus);

        await service.SweepOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await bus.DidNotReceiveWithAnyArgs()
            .PublishAsync(Arg.Any<ClaimCertificateExpiringEvent>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task SweepOnceAsync_BindingExpiringSoon_PublishesEvent()
    {
        AwsThingBinding binding = NewBinding(claimExpiresAt: Now.AddDays(10));
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([binding]);
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ClaimCertificateRotationCheckService service = NewService(reader, bus);

        await service.SweepOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await bus.Received(1)
            .PublishAsync(
                Arg.Is<ClaimCertificateExpiringEvent>(e => e.DeviceId == binding.DeviceId && e.DaysUntilExpiry == 10),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task SweepOnceAsync_BindingFarFromExpiry_DoesNotPublish()
    {
        AwsThingBinding binding = NewBinding(claimExpiresAt: Now.AddDays(60));
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([binding]);
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ClaimCertificateRotationCheckService service = NewService(reader, bus);

        await service.SweepOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await bus.DidNotReceiveWithAnyArgs()
            .PublishAsync(Arg.Any<ClaimCertificateExpiringEvent>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task SweepOnceAsync_BindingWithoutExpiry_Skipped()
    {
        AwsThingBinding binding = NewBinding(claimExpiresAt: null);
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([binding]);
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ClaimCertificateRotationCheckService service = NewService(reader, bus);

        await service.SweepOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        await bus.DidNotReceiveWithAnyArgs()
            .PublishAsync(Arg.Any<ClaimCertificateExpiringEvent>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task SweepOnceAsync_ReaderThrows_LogsButDoesNotPropagate()
    {
        IAwsThingBindingReader reader = Substitute.For<IAwsThingBindingReader>();
        reader.ListByStatusAsync(Arg.Any<IReadOnlyList<AwsThingProvisioningStatus>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<AwsThingBinding>>(_ => throw new InvalidOperationException("boom"));
        ILocalEventBus bus = Substitute.For<ILocalEventBus>();

        ClaimCertificateRotationCheckService service = NewService(reader, bus);

        await service.SweepOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        // No assertion needed: exception is swallowed and logged.
    }

    private static AwsThingBinding NewBinding(DateTimeOffset? claimExpiresAt)
    {
        var b = AwsThingBinding.Create(
            Guid.NewGuid(), tenantId: null,
            ThingName.Create($"t{Guid.NewGuid():N}-sn"));
        if (claimExpiresAt is { } v)
        {
            b.RecordClaimCertificateExpiry(v);
        }
        return b;
    }

    private static ClaimCertificateRotationCheckService NewService(IAwsThingBindingReader reader, ILocalEventBus bus)
    {
        ServiceCollection services = new();
        services.AddSingleton(reader);
        services.AddSingleton(bus);
        ServiceProvider provider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        FleetProvisioningOptions opts = new() { ExpiryWarningWindowDays = 30, RotationCheckBatchSize = 100, RotationCheckIntervalHours = 24 };
        IOptions<FleetProvisioningOptions> wrapper = Microsoft.Extensions.Options.Options.Create(opts);

        return new ClaimCertificateRotationCheckService(
            scopeFactory,
            wrapper,
            new IoTAwsFleetProvisioningMetrics(new TestMeterFactory()),
            NullLogger<ClaimCertificateRotationCheckService>.Instance,
            new FakeTimeProvider(Now));
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
