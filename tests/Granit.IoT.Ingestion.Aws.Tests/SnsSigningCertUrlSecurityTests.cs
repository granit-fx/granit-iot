using Granit.IoT.Ingestion.Aws.Internal;
using Granit.IoT.Ingestion.Aws.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using ZiggyCreatures.Caching.Fusion;

namespace Granit.IoT.Ingestion.Aws.Tests;

/// <summary>
/// Story #43 — red-team tests against the <c>DefaultSnsSigningCertificateCache</c>
/// CDN allow-list regex. A malicious SNS envelope can carry any
/// <c>SigningCertURL</c> value; if the regex lets a URL through, the validator
/// will fetch and trust whatever PEM sits at that endpoint. Each vector below is
/// a known attack shape; all must be rejected before any HTTP call is made.
/// </summary>
public sealed class SnsSigningCertUrlSecurityTests : IDisposable
{
    // Shared because DefaultSnsSigningCertificateCache only pulls the HttpClient
    // after the URL passes the allow-list — every test here short-circuits before
    // that, so the client is never actually used over the wire. Shared + disposed
    // in one place avoids the CodeQL "created but not disposed" warning without
    // cluttering each test with its own lifetime.
    private static readonly HttpClient SharedHttpClient = new();

    /// <summary>
    /// Attack matrix covering every shape the regex must reject. Each vector
    /// ships with the threat it represents; when a vector is added, update the
    /// doc comment rather than renaming inline.
    /// </summary>
    public static readonly TheoryData<string, string> AttackVectors = new()
    {
        // DoD-mandated vectors (AC 1-3)
        { "path-traversal", "https://sns.eu-west-1.amazonaws.com/../evil/cert.pem" },
        { "subdomain-confusion", "https://sns.eu-west-1.amazonaws.com.evil.com/SimpleNotificationService-abc.pem" },
        { "double-encoding", "https://sns.eu-west-1.amazonaws.com%2F..%2Fevil%2Fcert.pem" },

        // Scheme / authority manipulation
        { "http-downgrade", "http://sns.eu-west-1.amazonaws.com/SimpleNotificationService-abc.pem" },
        { "attacker-host", "https://evil.com/SimpleNotificationService-abc.pem" },
        { "userinfo-injection", "https://sns.eu-west-1.amazonaws.com@evil.com/SimpleNotificationService-abc.pem" },

        // Region / path manipulation
        { "no-region", "https://sns.amazonaws.com/SimpleNotificationService-abc.pem" },
        { "wrong-path-prefix", "https://sns.eu-west-1.amazonaws.com/something-else.pem" },
        { "evil-extension", "https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-abc.pem.evil" },
        { "null-byte", "https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-abc.pem\u0000evil" },
    };

    [Theory]
    [MemberData(nameof(AttackVectors))]
    public async Task Rejects_malicious_SigningCertURL(string attackName, string url)
    {
        _ = attackName;
        DefaultSnsSigningCertificateCache cache = BuildCache();

        await Should.ThrowAsync<InvalidOperationException>(
            () => cache.GetPublicKeyAsync(url, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Legitimate_CDN_url_is_allowed_by_the_regex()
    {
        // Positive control: the allow-list is not so tight that it rejects a
        // well-formed AWS URL. Failure must come from the HTTP layer, not the
        // regex — otherwise the validator would be a denial-of-service vector.
        DefaultSnsSigningCertificateCache cache = BuildCache();

        Exception? ex = await Record.ExceptionAsync(() => cache.GetPublicKeyAsync(
            "https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-0123abcd.pem",
            TestContext.Current.CancellationToken));

        ex.ShouldNotBeOfType<InvalidOperationException>(
            "Valid CDN URL must pass the allow-list; failure should come from the HTTP layer.");
    }

    /// <inheritdoc/>
    public void Dispose() => SharedHttpClient.Dispose();

    private static DefaultSnsSigningCertificateCache BuildCache()
    {
        IFusionCache fusionCache = Substitute.For<IFusionCache>();
        IHttpClientFactory clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(SharedHttpClient);

        IOptionsMonitor<AwsIoTIngestionOptions> optionsMonitor =
            Substitute.For<IOptionsMonitor<AwsIoTIngestionOptions>>();
        optionsMonitor.CurrentValue.Returns(new AwsIoTIngestionOptions
        {
            Sns = new AwsIoTSnsIngestionOptions
            {
                Enabled = true,
                Region = "eu-west-1",
                CertCacheHours = 24,
            },
        });

        return new DefaultSnsSigningCertificateCache(
            fusionCache,
            clientFactory,
            optionsMonitor,
            NullLogger<DefaultSnsSigningCertificateCache>.Instance);
    }
}
