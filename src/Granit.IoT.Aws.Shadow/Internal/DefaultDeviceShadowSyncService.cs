using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.IotData;
using Amazon.IotData.Model;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.Shadow.Abstractions;
using Granit.IoT.Aws.Shadow.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Granit.IoT.Aws.Shadow.Internal;

internal sealed class DefaultDeviceShadowSyncService(
    IAmazonIotData iotData,
    IoTAwsShadowMetrics metrics,
    ILogger<DefaultDeviceShadowSyncService> logger)
    : IDeviceShadowSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonIotData _iotData = iotData;
    private readonly IoTAwsShadowMetrics _metrics = metrics;
    private readonly ILogger<DefaultDeviceShadowSyncService> _logger = logger;

    public async Task PushReportedAsync(
        ThingName thingName,
        IReadOnlyDictionary<string, object?> reported,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thingName);
        ArgumentNullException.ThrowIfNull(reported);

        // AWS shadow update format: {"state":{"reported":{...}}}.
        var payload = new { state = new { reported } };
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);

        using var stream = new MemoryStream(bytes, writable: false);
        try
        {
            await _iotData.UpdateThingShadowAsync(
                new UpdateThingShadowRequest
                {
                    ThingName = thingName.Value,
                    Payload = stream,
                },
                cancellationToken).ConfigureAwait(false);

            _metrics.RecordReportedPushed(thingName.GetTenantId());
            ShadowLog.ReportedPushed(_logger, thingName.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _metrics.RecordUpdateFailed(thingName.GetTenantId());
            ShadowLog.ReportedPushFailed(_logger, thingName.Value, ex);
            throw;
        }
    }

    public async Task<DeviceShadowSnapshot?> GetShadowAsync(
        ThingName thingName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thingName);

        GetThingShadowResponse response;
        try
        {
            response = await _iotData.GetThingShadowAsync(
                new GetThingShadowRequest { ThingName = thingName.Value },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Amazon.IotData.Model.ResourceNotFoundException)
        {
            return null;
        }

        if (response.Payload is null || response.Payload.Length == 0)
        {
            return null;
        }

        return Parse(response.Payload);
    }

    private static DeviceShadowSnapshot Parse(MemoryStream payload)
    {
        payload.Position = 0;
        var root = JsonNode.Parse(payload);
        JsonNode? state = root?["state"];
        if (state is null)
        {
            return new DeviceShadowSnapshot(
                Empty(),
                Empty(),
                Empty(),
                Version: 0);
        }

        long version = root!["version"]?.GetValue<long>() ?? 0;
        return new DeviceShadowSnapshot(
            ReadFlatObject(state["reported"]),
            ReadFlatObject(state["desired"]),
            ReadFlatObject(state["delta"]),
            version);
    }

    private static Dictionary<string, object?> ReadFlatObject(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            return Empty();
        }

        var result = new Dictionary<string, object?>(obj.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, JsonNode?> pair in obj)
        {
            result[pair.Key] = pair.Value switch
            {
                null => null,
                JsonValue value when value.TryGetValue(out string? s) => s,
                JsonValue value when value.TryGetValue(out long l) => l,
                JsonValue value when value.TryGetValue(out double d) => d,
                JsonValue value when value.TryGetValue(out bool b) => b,
                _ => pair.Value.ToJsonString(),
            };
        }
        return result;
    }

    private static Dictionary<string, object?> Empty() =>
        new(0, StringComparer.Ordinal);
}
