using System.Buffers;
using System.Text.Json;
using System.Threading;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Mqtt;
using Granit.IoT.Mqtt.Internal;
using Granit.IoT.Mqtt.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using Polly;
using Polly.Retry;

namespace Granit.IoT.Mqtt.Mqttnet.Internal;

/// <summary>
/// MQTTnet v5 implementation of <see cref="IIoTMqttBridge"/>. Connects to a broker
/// over mTLS using a certificate loaded from <c>Granit.Vault</c>, subscribes to the
/// configured topic pattern, and forwards each inbound message through
/// <see cref="IIngestionPipeline"/> after wrapping it in a <see cref="MqttIngestionEnvelope"/>.
/// </summary>
internal sealed partial class MqttnetIoTBridge : IIoTMqttBridge, IHostedService, IAsyncDisposable
{
    private readonly IIngestionPipeline _pipeline;
    private readonly FeatureFlagSnapshot _featureFlag;
    private readonly ICertificateLoader _certificateLoader;
    private readonly ISettingsTopicResolver _topicResolver;
    private readonly IOptionsMonitor<IoTMqttOptions> _options;
    private readonly IoTMqttBridgeMetrics _metrics;
    private readonly TimeProvider _clock;
    private readonly ILogger<MqttnetIoTBridge> _logger;

    private readonly MqttClientFactory _factory = new();
    private readonly Lock _gate = new();
    private IMqttClient? _client;
    private MqttClientOptions? _clientOptions;
    private LoadedCertificate? _loadedCertificate;
    private ITimer? _expiryTimer;
    private ResiliencePipeline? _reconnectPipeline;
    private CancellationTokenSource? _shutdown;

    public MqttnetIoTBridge(
        IIngestionPipeline pipeline,
        FeatureFlagSnapshot featureFlag,
        ICertificateLoader certificateLoader,
        ISettingsTopicResolver topicResolver,
        IOptionsMonitor<IoTMqttOptions> options,
        IoTMqttBridgeMetrics metrics,
        TimeProvider clock,
        ILogger<MqttnetIoTBridge> logger)
    {
        _pipeline = pipeline;
        _featureFlag = featureFlag;
        _certificateLoader = certificateLoader;
        _topicResolver = topicResolver;
        _options = options;
        _metrics = metrics;
        _clock = clock;
        _logger = logger;
    }

    public MqttBridgeStatus Status { get; private set; } = MqttBridgeStatus.Stopped;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Status = MqttBridgeStatus.Starting;
        _shutdown = new CancellationTokenSource();
        IoTMqttOptions opts = _options.CurrentValue;
        EnsureSecureBrokerUri(opts.BrokerUri);

        _reconnectPipeline = BuildReconnectPipeline();
        _client = _factory.CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;

        await ReloadCertificateAndOptionsAsync(cancellationToken).ConfigureAwait(false);
        await ConnectAndSubscribeAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_shutdown is { } cts)
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }

        if (_expiryTimer is { } timer)
        {
            await timer.DisposeAsync().ConfigureAwait(false);
            _expiryTimer = null;
        }

        if (_client is { IsConnected: true })
        {
            try
            {
                await _client.DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogDisconnectFailure(_logger, ex.Message);
            }
        }

        Status = MqttBridgeStatus.Stopped;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        if (_client is { } client)
        {
            client.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync;
            client.DisconnectedAsync -= OnDisconnectedAsync;
            client.Dispose();
        }

        _shutdown?.Dispose();
        _loadedCertificate?.Certificate.Dispose();
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        _metrics.RecordReceived();
        CancellationToken ct = _shutdown?.Token ?? CancellationToken.None;

        if (!await _featureFlag.IsEnabledAsync(ct).ConfigureAwait(false))
        {
            _metrics.RecordFeatureDisabled();
            return;
        }

        IoTMqttOptions opts = _options.CurrentValue;
        ReadOnlySequence<byte> payload = e.ApplicationMessage.Payload;
        if (payload.Length > opts.MaxPayloadBytes)
        {
            LogPayloadTooLarge(_logger, payload.Length, opts.MaxPayloadBytes);
            _metrics.RecordDispatched("oversized");
            return;
        }

        ReadOnlyMemory<byte> body = SerializeEnvelope(e, _clock.GetUtcNow());

        try
        {
            IngestionResult result = await _pipeline
                .ProcessAsync(MqttConstants.SourceName, body, EmptyHeaders.Instance, ct)
                .ConfigureAwait(false);
            _metrics.RecordDispatched(result.Outcome.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogPipelineFailure(_logger, ex.Message);
            _metrics.RecordDispatched("pipeline_failure");
        }
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        if (_shutdown is null || _shutdown.IsCancellationRequested)
        {
            return;
        }

        Status = MqttBridgeStatus.Reconnecting;
        LogDisconnected(_logger, args.Reason.ToString());

        try
        {
            await _reconnectPipeline!
                .ExecuteAsync(async ct => await ConnectAndSubscribeAsync(ct).ConfigureAwait(false), _shutdown.Token)
                .ConfigureAwait(false);
            Status = MqttBridgeStatus.Connected;
        }
        catch (OperationCanceledException)
        {
            // Shutdown — leave Status as Reconnecting until StopAsync flips it.
        }
        catch (Exception ex)
        {
            Status = MqttBridgeStatus.Faulted;
            LogReconnectExhausted(_logger, ex.Message);
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client!.ConnectAsync(_clientOptions!, cancellationToken).ConfigureAwait(false);
            string topic = await _topicResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
            await _client.SubscribeAsync(topic, ToQos(_options.CurrentValue.DefaultQoS), cancellationToken)
                .ConfigureAwait(false);
            Status = MqttBridgeStatus.Connected;
            LogConnected(_logger, topic);
        }
        catch (Exception ex)
        {
            _metrics.RecordConnectionFailure();
            LogConnectionFailure(_logger, ex.Message);
            throw;
        }
    }

    private async Task ReloadCertificateAndOptionsAsync(CancellationToken cancellationToken)
    {
        LoadedCertificate cert = await _certificateLoader.LoadAsync(cancellationToken).ConfigureAwait(false);
        IoTMqttOptions opts = _options.CurrentValue;

        Uri brokerUri = new(opts.BrokerUri, UriKind.Absolute);
        MqttClientOptions newOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(brokerUri.Host, brokerUri.Port == -1 ? 8883 : brokerUri.Port)
            .WithClientId(opts.ClientId)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(opts.KeepAliveSeconds))
            .WithTlsOptions(o => o
                .UseTls(true)
                .WithClientCertificates([cert.Certificate])
                .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13))
            .Build();

        LoadedCertificate? previous;
        lock (_gate)
        {
            previous = _loadedCertificate;
            _loadedCertificate = cert;
            _clientOptions = newOptions;
        }

        previous?.Certificate.Dispose();
        ScheduleExpiryTimer(cert.ExpiresOn, opts.CertificateExpiryWarningMinutes);
    }

    private void ScheduleExpiryTimer(DateTimeOffset? expiresOn, int warningMinutes)
    {
        _expiryTimer?.Dispose();
        if (expiresOn is not { } when_)
        {
            return;
        }

        TimeSpan delay = when_ - _clock.GetUtcNow() - TimeSpan.FromMinutes(warningMinutes);
        if (delay <= TimeSpan.Zero)
        {
            // Already past the warning threshold — fire on the next tick.
            delay = TimeSpan.FromSeconds(1);
        }

        _expiryTimer = _clock.CreateTimer(_ => _ = OnCertificateExpiringAsync(),
            state: null, dueTime: delay, period: Timeout.InfiniteTimeSpan);
    }

    private async Task OnCertificateExpiringAsync()
    {
        if (_shutdown is null || _shutdown.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _metrics.RecordCertificateReload();
            LogCertificateReloading(_logger);
            await ReloadCertificateAndOptionsAsync(_shutdown.Token).ConfigureAwait(false);

            if (_client is { IsConnected: true })
            {
                await _client.DisconnectAsync(cancellationToken: _shutdown.Token).ConfigureAwait(false);
                // The DisconnectedAsync handler will reconnect with the new certificate.
            }
        }
        catch (Exception ex)
        {
            LogCertificateReloadFailure(_logger, ex.Message);
        }
    }

    private static MqttQualityOfServiceLevel ToQos(int level) => level switch
    {
        0 => MqttQualityOfServiceLevel.AtMostOnce,
        2 => MqttQualityOfServiceLevel.ExactlyOnce,
        _ => MqttQualityOfServiceLevel.AtLeastOnce,
    };

    private static void EnsureSecureBrokerUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsed)
            || !string.Equals(parsed.Scheme, "mqtts", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "IoT:Mqtt:BrokerUri must use the 'mqtts://' scheme — plaintext MQTT is forbidden by the bridge.");
        }
    }

    private ResiliencePipeline BuildReconnectPipeline() =>
        new ResiliencePipelineBuilder { TimeProvider = _clock }
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = int.MaxValue,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                OnRetry = _ =>
                {
                    _metrics.RecordReconnectAttempt();
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

    private static ReadOnlyMemory<byte> SerializeEnvelope(
        MqttApplicationMessageReceivedEventArgs e,
        DateTimeOffset now)
    {
        InnerPayload? inner = TryDeserializeInner(e.ApplicationMessage.Payload);
        MqttIngestionEnvelope envelope = new()
        {
            MessageId = $"{e.ClientId}:{e.PacketIdentifier}",
            Topic = e.ApplicationMessage.Topic,
            Qos = (int)e.ApplicationMessage.QualityOfServiceLevel,
            Retain = e.ApplicationMessage.Retain,
            ClientId = e.ClientId,
            Timestamp = now,
            Payload = inner,
        };

        return JsonSerializer.SerializeToUtf8Bytes(envelope, MqttJsonContext.Default.MqttIngestionEnvelope);
    }

    private static InnerPayload? TryDeserializeInner(in ReadOnlySequence<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return null;
        }

        try
        {
            if (payload.IsSingleSegment)
            {
                return JsonSerializer.Deserialize(payload.FirstSpan, MqttJsonContext.Default.InnerPayload);
            }

            byte[] copy = payload.ToArray();
            return JsonSerializer.Deserialize(copy, MqttJsonContext.Default.InnerPayload);
        }
        catch (JsonException)
        {
            // Let the parser surface a clean ParseFailure outcome via the pipeline.
            return null;
        }
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "MQTT bridge connected; subscribed to '{Topic}'.")]
    private static partial void LogConnected(ILogger logger, string topic);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Warning, Message = "MQTT broker disconnected ({Reason}); attempting reconnect.")]
    private static partial void LogDisconnected(ILogger logger, string reason);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Error, Message = "MQTT reconnect pipeline exhausted: {Reason}.")]
    private static partial void LogReconnectExhausted(ILogger logger, string reason);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Error, Message = "MQTT broker connection failed: {Reason}.")]
    private static partial void LogConnectionFailure(ILogger logger, string reason);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Warning, Message = "Clean disconnect failed: {Reason}.")]
    private static partial void LogDisconnectFailure(ILogger logger, string reason);

    [LoggerMessage(EventId = 5006, Level = LogLevel.Warning, Message = "Dropping MQTT message: payload length {Length} exceeds the configured cap {Limit}.")]
    private static partial void LogPayloadTooLarge(ILogger logger, long length, int limit);

    [LoggerMessage(EventId = 5007, Level = LogLevel.Error, Message = "Ingestion pipeline raised while processing an MQTT message: {Reason}.")]
    private static partial void LogPipelineFailure(ILogger logger, string reason);

    [LoggerMessage(EventId = 5008, Level = LogLevel.Information, Message = "Reloading MQTT client certificate ahead of expiry.")]
    private static partial void LogCertificateReloading(ILogger logger);

    [LoggerMessage(EventId = 5009, Level = LogLevel.Error, Message = "Failed to reload MQTT client certificate: {Reason}.")]
    private static partial void LogCertificateReloadFailure(ILogger logger, string reason);

    private static class EmptyHeaders
    {
        public static readonly IReadOnlyDictionary<string, string> Instance =
            new Dictionary<string, string>(capacity: 0, StringComparer.OrdinalIgnoreCase);
    }
}
