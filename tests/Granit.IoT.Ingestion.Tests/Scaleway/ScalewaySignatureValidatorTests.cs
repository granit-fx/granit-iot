#pragma warning disable CA2012 // NSubstitute ValueTask setup — consumed exactly once per call
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Scaleway.Internal;
using Granit.IoT.Ingestion.Scaleway.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Scaleway;

public sealed class ScalewaySignatureValidatorTests
{
    private const string Secret = "0123456789abcdef0123456789abcdef";
    private static readonly byte[] Body = Encoding.UTF8.GetBytes("""{"hello":"world"}""");

    [Fact]
    public async Task ValidateAsync_MatchingSignature_IsValid()
    {
        ScalewaySignatureValidator validator = BuildValidator(Secret);
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Scaleway-Signature"] = ComputeSignature(Secret, Body),
        };

        SignatureValidationResult result = await validator
            .ValidateAsync(Body, headers, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_TamperedBody_IsInvalid()
    {
        ScalewaySignatureValidator validator = BuildValidator(Secret);
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Scaleway-Signature"] = ComputeSignature(Secret, Body),
        };

        byte[] tampered = Encoding.UTF8.GetBytes("""{"hello":"intruder"}""");

        SignatureValidationResult result = await validator
            .ValidateAsync(tampered, headers, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAsync_MissingHeader_IsInvalid()
    {
        ScalewaySignatureValidator validator = BuildValidator(Secret);

        SignatureValidationResult result = await validator
            .ValidateAsync(Body, new Dictionary<string, string>(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.ShouldBeFalse();
        result.FailureReason.ShouldNotBeNull().ShouldContain("X-Scaleway-Signature");
    }

    [Fact]
    public async Task ValidateAsync_WrongSecret_IsInvalid()
    {
        ScalewaySignatureValidator validator = BuildValidator(Secret);
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Scaleway-Signature"] = ComputeSignature("a-different-secret", Body),
        };

        SignatureValidationResult result = await validator
            .ValidateAsync(Body, headers, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAsync_MalformedHexHeader_IsInvalid()
    {
        ScalewaySignatureValidator validator = BuildValidator(Secret);
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Scaleway-Signature"] = "not-hex-not-the-right-length",
        };

        SignatureValidationResult result = await validator
            .ValidateAsync(Body, headers, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsValid.ShouldBeFalse();
    }

    private static ScalewaySignatureValidator BuildValidator(string secret)
    {
        IOptionsMonitor<ScalewayIoTOptions> monitor = Substitute.For<IOptionsMonitor<ScalewayIoTOptions>>();
        monitor.CurrentValue.Returns(new ScalewayIoTOptions { SharedSecret = secret });
        return new ScalewaySignatureValidator(monitor);
    }

    private static string ComputeSignature(string secret, ReadOnlySpan<byte> body)
    {
        Span<byte> digest = stackalloc byte[HMACSHA256.HashSizeInBytes];
        int written = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body, digest);

        StringBuilder sb = new(written * 2);
        foreach (byte b in digest[..written])
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}
