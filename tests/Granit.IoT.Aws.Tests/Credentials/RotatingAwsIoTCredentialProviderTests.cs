using Granit.IoT.Aws.Credentials;
using Granit.IoT.Aws.Credentials.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Granit.IoT.Aws.Tests.Credentials;

public sealed class RotatingAwsIoTCredentialProviderTests
{
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly ILogger<RotatingAwsIoTCredentialProvider> _logger =
        NullLogger<RotatingAwsIoTCredentialProvider>.Instance;

    [Fact]
    public async Task InitialFetch_PopulatesCredentialsAndFlipsReady()
    {
        IAwsIoTCredentialLoader loader = Substitute.For<IAwsIoTCredentialLoader>();
        loader.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(new LoadedAwsIoTCredentials("AKIA-V1", "SECRET-V1"));

        using RotatingAwsIoTCredentialProvider provider = NewProvider(loader);
        await provider.StartAsync(TestContext.Current.CancellationToken);
        await WaitForAsync(() => provider.IsReady);

        provider.IsReady.ShouldBeTrue();
        provider.AccessKeyId.ShouldBe("AKIA-V1");
        provider.SecretAccessKey.ShouldBe("SECRET-V1");
        provider.SessionToken.ShouldBeNull();
    }

    [Fact]
    public async Task LoaderReturnsNull_KeepsReadyAndFallsBackToSdkChain()
    {
        IAwsIoTCredentialLoader loader = Substitute.For<IAwsIoTCredentialLoader>();
        loader.LoadAsync(Arg.Any<CancellationToken>()).Returns((LoadedAwsIoTCredentials?)null);

        using RotatingAwsIoTCredentialProvider provider = NewProvider(loader);
        await provider.StartAsync(TestContext.Current.CancellationToken);
        await WaitForAsync(() => provider.IsReady);

        provider.IsReady.ShouldBeTrue();
        provider.AccessKeyId.ShouldBeNull();
        provider.SecretAccessKey.ShouldBeNull();
    }

    [Fact]
    public async Task FailedInitialFetch_LeavesProviderNotReady()
    {
        IAwsIoTCredentialLoader loader = Substitute.For<IAwsIoTCredentialLoader>();
        loader.LoadAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Secrets Manager unreachable"));

        using RotatingAwsIoTCredentialProvider provider = NewProvider(loader);
        await provider.StartAsync(TestContext.Current.CancellationToken);
        // Wait for the loader to be invoked at least once so the failure path executed.
        await WaitForAsync(() => loader.ReceivedCalls().Any());

        provider.IsReady.ShouldBeFalse();
        provider.AccessKeyId.ShouldBeNull();
    }

    [Fact]
    public async Task RotationDetected_ReplacesCachedCredentials()
    {
        IAwsIoTCredentialLoader loader = Substitute.For<IAwsIoTCredentialLoader>();
        loader.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(
                new LoadedAwsIoTCredentials("AKIA-V1", "SECRET-V1"),
                new LoadedAwsIoTCredentials("AKIA-V2", "SECRET-V2"));

        using RotatingAwsIoTCredentialProvider provider = NewProvider(loader);
        await provider.StartAsync(TestContext.Current.CancellationToken);
        await WaitForAsync(() => provider.AccessKeyId == "AKIA-V1");

        _time.Advance(TimeSpan.FromMinutes(5));
        // Allow the BackgroundService loop to react to the timer tick.
        await WaitForAsync(() => provider.AccessKeyId == "AKIA-V2");

        provider.AccessKeyId.ShouldBe("AKIA-V2");
        provider.SecretAccessKey.ShouldBe("SECRET-V2");
    }

    [Fact]
    public async Task RefreshFailureAfterSuccess_KeepsLastKnownCredentials()
    {
        IAwsIoTCredentialLoader loader = Substitute.For<IAwsIoTCredentialLoader>();
        loader.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromResult<LoadedAwsIoTCredentials?>(new LoadedAwsIoTCredentials("AKIA-V1", "SECRET-V1")),
                _ => Task.FromException<LoadedAwsIoTCredentials?>(new TimeoutException("transient")));

        using RotatingAwsIoTCredentialProvider provider = NewProvider(loader);
        await provider.StartAsync(TestContext.Current.CancellationToken);
        await WaitForAsync(() => provider.AccessKeyId == "AKIA-V1");

        _time.Advance(TimeSpan.FromMinutes(5));
        await WaitForAsync(() => loader.ReceivedCalls().Count() >= 2);

        provider.IsReady.ShouldBeTrue();
        provider.AccessKeyId.ShouldBe("AKIA-V1");
        provider.SecretAccessKey.ShouldBe("SECRET-V1");
    }

    private RotatingAwsIoTCredentialProvider NewProvider(IAwsIoTCredentialLoader loader) =>
        new(loader,
            Options.Create(new AwsIoTCredentialOptions
            {
                FleetCredentialSecretArn = "arn:aws:secretsmanager:eu-west-1:123:secret:fleet",
                RotationCheckIntervalMinutes = 5,
                InitialFetchTimeoutSeconds = 5,
            }),
            _logger,
            _time);

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }
}
