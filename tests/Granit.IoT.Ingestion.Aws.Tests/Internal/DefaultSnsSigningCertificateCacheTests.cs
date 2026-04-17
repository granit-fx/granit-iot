using Granit.IoT.Ingestion.Aws.Internal;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using ZiggyCreatures.Caching.Fusion;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal;

public class DefaultSnsSigningCertificateCacheTests
{
    [Theory]
    [InlineData("https://evil.com/SimpleNotificationService-abc.pem")]
    [InlineData("http://sns.eu-west-1.amazonaws.com/SimpleNotificationService-abc.pem")]  // not https
    [InlineData("https://sns.amazonaws.com/SimpleNotificationService-abc.pem")]  // no region
    [InlineData("https://sns.eu-west-1.amazonaws.com/something-else.pem")]
    [InlineData("https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-abc.pem.evil")]
    public async Task GetPublicKeyAsync_rejects_urls_outside_the_AWS_CDN_allow_list(string url)
    {
        DefaultSnsSigningCertificateCache cache = BuildCache();

        await Should.ThrowAsync<InvalidOperationException>(
            () => cache.GetPublicKeyAsync(url, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetPublicKeyAsync_allows_valid_CDN_urls_but_fails_on_fetch()
    {
        // The URL passes the regex; the fetch itself fails because the HttpClient has no handler.
        // This proves the allow-list doesn't block legitimate URLs.
        DefaultSnsSigningCertificateCache cache = BuildCache();

        Exception? ex = await Record.ExceptionAsync(() => cache.GetPublicKeyAsync(
            "https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-abc123.pem",
            TestContext.Current.CancellationToken));

        ex.ShouldNotBeOfType<InvalidOperationException>(
            "Valid CDN URL should pass the allow-list; failure should come from the HTTP layer.");
    }

    private static DefaultSnsSigningCertificateCache BuildCache()
    {
        IFusionCache fusionCache = Substitute.For<IFusionCache>();
        IHttpClientFactory clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        IOptionsMonitor<AwsIoTIngestionOptions> optionsMonitor =
            Substitute.For<IOptionsMonitor<AwsIoTIngestionOptions>>();
        optionsMonitor.CurrentValue.Returns(new AwsIoTIngestionOptions
        {
            Sns = { Enabled = true, Region = "eu-west-1", CertCacheHours = 24 },
        });

        return new DefaultSnsSigningCertificateCache(
            fusionCache,
            clientFactory,
            optionsMonitor,
            NullLogger<DefaultSnsSigningCertificateCache>.Instance);
    }
}
