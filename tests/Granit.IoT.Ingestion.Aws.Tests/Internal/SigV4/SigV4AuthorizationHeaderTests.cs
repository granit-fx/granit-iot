using Granit.IoT.Ingestion.Aws.Internal.SigV4;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal.SigV4;

public class SigV4AuthorizationHeaderTests
{
    private const string ValidHeader =
        "AWS4-HMAC-SHA256 " +
        "Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, " +
        "SignedHeaders=host;x-amz-date, " +
        "Signature=5fa00fa31553b73ebf1942676e86291e8372ff2a2260956d9b8aae1d763fbf31";

    [Fact]
    public void TryParse_splits_a_well_formed_header_into_its_three_components()
    {
        var parsed = SigV4AuthorizationHeader.TryParse(ValidHeader);

        parsed.ShouldNotBeNull();
        parsed!.AccessKeyId.ShouldBe("AKIDEXAMPLE");
        parsed.Scope.ShouldBe(new SigV4Scope("20150830", "us-east-1", "service"));
        parsed.SignedHeaders.ShouldBe(new[] { "host", "x-amz-date" });
        parsed.Signature.ShouldBe("5fa00fa31553b73ebf1942676e86291e8372ff2a2260956d9b8aae1d763fbf31");
    }

    [Fact]
    public void TryParse_lowercases_SignedHeader_names()
    {
        string header = ValidHeader.Replace("host;x-amz-date", "HOST;X-Amz-Date", StringComparison.Ordinal);
        var parsed = SigV4AuthorizationHeader.TryParse(header);
        parsed.ShouldNotBeNull();
        parsed!.SignedHeaders.ShouldBe(new[] { "host", "x-amz-date" });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AWS2-HMAC-SHA1 Credential=x, SignedHeaders=y, Signature=z")]   // wrong algorithm
    [InlineData("AWS4-HMAC-SHA256 Credential=AKID/20150830/us-east-1/service, SignedHeaders=host, Signature=abc")]  // credential missing terminator (4 parts)
    [InlineData("AWS4-HMAC-SHA256 Credential=AKID/20150830/us-east-1/service/aws4_request, SignedHeaders=host")]    // missing Signature
    [InlineData("AWS4-HMAC-SHA256 Credential=AKID/20150830/us-east-1/service/aws4_request, Signature=abc")]         // missing SignedHeaders
    public void TryParse_returns_null_on_malformed_inputs(string? header)
    {
        SigV4AuthorizationHeader.TryParse(header).ShouldBeNull();
    }
}
