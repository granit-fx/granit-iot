using System.Diagnostics.Metrics;
using Amazon.IoT;
using Amazon.IoT.Model;
using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Jobs.Abstractions;
using Granit.IoT.Aws.Jobs.Diagnostics;
using Granit.IoT.Aws.Jobs.Internal;
using Granit.IoT.Aws.Jobs.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Granit.IoT.Aws.Jobs.Tests.Internal;

public sealed class AwsIoTJobsCommandDispatcherTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string ThingArn = "arn:aws:iot:eu-west-1:123:thing/sample";

    private readonly IAmazonIoT _iot = Substitute.For<IAmazonIoT>();
    private readonly InMemoryJobTrackingStore _tracking = new(new FakeTimeProvider(DateTimeOffset.UtcNow));
    private readonly IAwsIoTCredentialProvider _credentials = Substitute.For<IAwsIoTCredentialProvider>();
    private readonly IoTAwsJobsMetrics _metrics = new(new TestMeterFactory());

    public AwsIoTJobsCommandDispatcherTests() => _credentials.IsReady.Returns(true);

    [Fact]
    public async Task DispatchAsync_CreatesJob_OnFirstDispatch()
    {
        AwsIoTJobsCommandDispatcher dispatcher = NewDispatcher();
        FirmwareUpdateCommand cmd = NewCommand();

        string jobId = await dispatcher.DispatchAsync(
            cmd, DeviceCommandTarget.ForThing(ThingArn), TestContext.Current.CancellationToken);

        jobId.ShouldBe($"granit-{cmd.CorrelationId}");
        await _iot.Received(1).CreateJobAsync(
            Arg.Is<CreateJobRequest>(r => r.JobId == jobId
                && r.Targets.Single() == ThingArn
                && r.TargetSelection == TargetSelection.SNAPSHOT
                && r.Document.Contains("\"operation\":\"firmware.update\"")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_IsIdempotentOnReDispatch_ViaTrackingStore()
    {
        AwsIoTJobsCommandDispatcher dispatcher = NewDispatcher();
        FirmwareUpdateCommand cmd = NewCommand();
        await dispatcher.DispatchAsync(
            cmd, DeviceCommandTarget.ForThing(ThingArn), TestContext.Current.CancellationToken);

        await dispatcher.DispatchAsync(
            cmd, DeviceCommandTarget.ForThing(ThingArn), TestContext.Current.CancellationToken);

        await _iot.Received(1).CreateJobAsync(Arg.Any<CreateJobRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_TreatsAwsResourceAlreadyExistsAsIdempotentReuse()
    {
        AwsIoTJobsCommandDispatcher dispatcher = NewDispatcher();
        _iot.CreateJobAsync(Arg.Any<CreateJobRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Amazon.IoT.Model.ResourceAlreadyExistsException("dup"));

        FirmwareUpdateCommand cmd = NewCommand();
        string jobId = await dispatcher.DispatchAsync(
            cmd, DeviceCommandTarget.ForThing(ThingArn), TestContext.Current.CancellationToken);

        jobId.ShouldBe($"granit-{cmd.CorrelationId}");
    }

    [Fact]
    public async Task DispatchAsync_RefusesWhenCredentialsNotReady()
    {
        _credentials.IsReady.Returns(false);
        AwsIoTJobsCommandDispatcher dispatcher = NewDispatcher();

        await Should.ThrowAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(
            NewCommand(),
            DeviceCommandTarget.ForThing(ThingArn),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_DynamicGroup_ReusesExistingGroup()
    {
        AwsIoTJobsCommandDispatcher dispatcher = NewDispatcher();
        _iot.DescribeThingGroupAsync(Arg.Any<DescribeThingGroupRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DescribeThingGroupResponse
            {
                ThingGroupArn = "arn:aws:iot:eu-west-1:123:thinggroup/granit-dynamic-deadbeefdeadbeef",
            });

        FirmwareUpdateCommand cmd = NewCommand();
        await dispatcher.DispatchAsync(
            cmd,
            DeviceCommandTarget.ForDynamicQuery("attributes.model:THERM-PRO"),
            TestContext.Current.CancellationToken);

        await _iot.DidNotReceive().CreateDynamicThingGroupAsync(
            Arg.Any<CreateDynamicThingGroupRequest>(), Arg.Any<CancellationToken>());
        await _iot.Received(1).CreateJobAsync(
            Arg.Is<CreateJobRequest>(r => r.TargetSelection == TargetSelection.CONTINUOUS),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_DynamicGroup_CreatesGroup_WhenAbsent()
    {
        AwsIoTJobsCommandDispatcher dispatcher = NewDispatcher();
        _iot.DescribeThingGroupAsync(Arg.Any<DescribeThingGroupRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Amazon.IoT.Model.ResourceNotFoundException("nope"));
        _iot.CreateDynamicThingGroupAsync(Arg.Any<CreateDynamicThingGroupRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateDynamicThingGroupResponse
            {
                ThingGroupArn = "arn:aws:iot:eu-west-1:123:thinggroup/granit-dynamic-cafebabecafebabe",
            });

        await dispatcher.DispatchAsync(
            NewCommand(),
            DeviceCommandTarget.ForDynamicQuery("attributes.model:THERM-PRO"),
            TestContext.Current.CancellationToken);

        await _iot.Received(1).CreateDynamicThingGroupAsync(
            Arg.Is<CreateDynamicThingGroupRequest>(r => r.ThingGroupName.StartsWith("granit-dynamic-")),
            Arg.Any<CancellationToken>());
    }

    private AwsIoTJobsCommandDispatcher NewDispatcher() =>
        new(_iot, _tracking, _credentials, MsOptions.Create(new AwsIoTJobsOptions()), _metrics,
            NullLogger<AwsIoTJobsCommandDispatcher>.Instance);

    private static FirmwareUpdateCommand NewCommand() => new(
        Guid.NewGuid(),
        Tenant,
        new Dictionary<string, object?>(StringComparer.Ordinal) { ["targetVersion"] = "2.1.0" });

    private sealed record FirmwareUpdateCommand(
        Guid CorrelationId,
        Guid? TenantId,
        IReadOnlyDictionary<string, object?> Parameters)
        : IDeviceCommand
    {
        public string Operation => "firmware.update";
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
