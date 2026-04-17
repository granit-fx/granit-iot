using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.IoT.Aws.Credentials.Internal;

/// <summary>
/// Background service that polls an <see cref="IAwsIoTCredentialLoader"/> on a
/// fixed interval and exposes the latest credential triplet through
/// <see cref="IAwsIoTCredentialProvider"/>. Reads are lock-free (volatile
/// fields). A failed refresh keeps the previous value in place — callers
/// always see a consistent snapshot, never a half-updated triplet.
/// </summary>
internal sealed class RotatingAwsIoTCredentialProvider(
    IAwsIoTCredentialLoader loader,
    IOptions<AwsIoTCredentialOptions> options,
    ILogger<RotatingAwsIoTCredentialProvider> logger,
    TimeProvider timeProvider)
    : BackgroundService, IAwsIoTCredentialProvider
{
    private readonly IAwsIoTCredentialLoader _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly AwsIoTCredentialOptions _options = options.Value;
    private readonly ILogger<RotatingAwsIoTCredentialProvider> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    private volatile string? _accessKeyId;
    private volatile string? _secretAccessKey;
    private volatile string? _sessionToken;
    private volatile bool _isReady;

    public string? AccessKeyId => _accessKeyId;

    public string? SecretAccessKey => _secretAccessKey;

    public string? SessionToken => _sessionToken;

    public bool IsReady => _isReady;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await TryInitialFetchAsync(stoppingToken).ConfigureAwait(false);

        var period = TimeSpan.FromMinutes(_options.RotationCheckIntervalMinutes);
        using var timer = new PeriodicTimer(period, _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await RefreshAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Host shutdown — expected.
        }
    }

    private async Task TryInitialFetchAsync(CancellationToken stoppingToken)
    {
        // The first fetch is bounded so a flaky Secrets Manager doesn't block
        // host startup forever. The provider stays not-ready until either the
        // initial fetch completes or a later rotation tick succeeds.
        var timeout = TimeSpan.FromSeconds(_options.InitialFetchTimeoutSeconds);
        using var timeoutCts = new CancellationTokenSource(timeout, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

        try
        {
            await RefreshAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            RotatingAwsIoTCredentialLog.InitialFetchTimedOut(_logger, _options.InitialFetchTimeoutSeconds);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            LoadedAwsIoTCredentials? loaded = await _loader.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (loaded is null)
            {
                // The loader explicitly signalled "use the SDK default chain".
                _accessKeyId = null;
                _secretAccessKey = null;
                _sessionToken = null;
                _isReady = true;
                return;
            }

            ApplyLoaded(loaded);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Stale-ok: keep whatever we already exposed (which may be nothing
            // on the very first fetch — IsReady stays false until success).
            RotatingAwsIoTCredentialLog.RefreshFailed(_logger, ex);
        }
    }

    private void ApplyLoaded(LoadedAwsIoTCredentials loaded)
    {
        bool wasReady = _isReady;
        string? previousAccessKey = _accessKeyId;

        _accessKeyId = loaded.AccessKeyId;
        _secretAccessKey = loaded.SecretAccessKey;
        _sessionToken = loaded.SessionToken;
        _isReady = true;

        // Access key ids are identifiers, not secrets — the secret value is the
        // SecretAccessKey, which never lands in a log message.
        if (!wasReady)
        {
            RotatingAwsIoTCredentialLog.CredentialsLoaded(_logger, loaded.AccessKeyId);
        }
        else if (!string.Equals(previousAccessKey, loaded.AccessKeyId, StringComparison.Ordinal))
        {
            RotatingAwsIoTCredentialLog.RotationDetected(_logger, loaded.AccessKeyId);
        }
    }
}
