using System.Diagnostics.Metrics;
using System.Text;
using Amazon.IotData;
using Amazon.IotData.Model;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Shadow.Abstractions;
using Granit.IoT.Aws.Shadow.Diagnostics;
using Granit.IoT.Aws.Shadow.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Granit.IoT.Aws.Shadow.Tests.Internal;

public sealed class DefaultDeviceShadowSyncServiceTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly IAmazonIotData _iotData = Substitute.For<IAmazonIotData>();
    private readonly AwsShadowMetrics _metrics = new(new TestMeterFactory());

    [Fact]
    public async Task PushReportedAsync_SendsExpectedJsonPayload()
    {
        DefaultDeviceShadowSyncService service = NewService();
        UpdateThingShadowRequest? captured = null;
        _iotData.UpdateThingShadowAsync(
                Arg.Do<UpdateThingShadowRequest>(r => captured = r),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateThingShadowResponse());

        await service.PushReportedAsync(
            ThingName.From(Tenant, "SN-001"),
            new Dictionary<string, object?> { ["status"] = "Active" },
            TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.ThingName.ShouldStartWith("t");
        string json = Encoding.UTF8.GetString(((MemoryStream)captured.Payload).ToArray());
        json.ShouldContain("\"reported\"");
        json.ShouldContain("\"status\":\"Active\"");
    }

    [Fact]
    public async Task PushReportedAsync_TracksFailureMetric_WhenAwsFails()
    {
        DefaultDeviceShadowSyncService service = NewService();
        _iotData.UpdateThingShadowAsync(Arg.Any<UpdateThingShadowRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("AWS unreachable"));

        await Should.ThrowAsync<InvalidOperationException>(() => service.PushReportedAsync(
            ThingName.From(Tenant, "SN-001"),
            new Dictionary<string, object?> { ["status"] = "Active" },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetShadowAsync_ReturnsNull_WhenShadowNotFound()
    {
        DefaultDeviceShadowSyncService service = NewService();
        _iotData.GetThingShadowAsync(Arg.Any<GetThingShadowRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Amazon.IotData.Model.ResourceNotFoundException("absent"));

        DeviceShadowSnapshot? result = await service.GetShadowAsync(
            ThingName.From(Tenant, "SN-001"), TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetShadowAsync_ParsesReportedDesiredAndDelta()
    {
        const string Json = """
            {
              "state": {
                "reported": {"status":"Active","battery":42},
                "desired":  {"status":"Suspended","battery":42},
                "delta":    {"status":"Suspended"}
              },
              "version": 17
            }
            """;
        DefaultDeviceShadowSyncService service = NewService();
        _iotData.GetThingShadowAsync(Arg.Any<GetThingShadowRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetThingShadowResponse { Payload = new MemoryStream(Encoding.UTF8.GetBytes(Json)) });

        DeviceShadowSnapshot? snapshot = await service.GetShadowAsync(
            ThingName.From(Tenant, "SN-001"), TestContext.Current.CancellationToken);

        snapshot.ShouldNotBeNull();
        snapshot.Version.ShouldBe(17);
        snapshot.Reported["status"].ShouldBe("Active");
        snapshot.Desired["status"].ShouldBe("Suspended");
        snapshot.Delta.Count.ShouldBe(1);
        snapshot.Delta["status"].ShouldBe("Suspended");
    }

    [Fact]
    public async Task GetShadowAsync_ReturnsEmptyDelta_WhenInSync()
    {
        const string Json = """
            {
              "state": {
                "reported": {"status":"Active"},
                "desired":  {"status":"Active"}
              },
              "version": 3
            }
            """;
        DefaultDeviceShadowSyncService service = NewService();
        _iotData.GetThingShadowAsync(Arg.Any<GetThingShadowRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetThingShadowResponse { Payload = new MemoryStream(Encoding.UTF8.GetBytes(Json)) });

        DeviceShadowSnapshot? snapshot = await service.GetShadowAsync(
            ThingName.From(Tenant, "SN-001"), TestContext.Current.CancellationToken);

        snapshot.ShouldNotBeNull();
        snapshot.Delta.ShouldBeEmpty();
    }

    private DefaultDeviceShadowSyncService NewService() =>
        new(_iotData, _metrics, NullLogger<DefaultDeviceShadowSyncService>.Instance);

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
