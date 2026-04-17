using System.Text;
using Granit.IoT.Ingestion.Aws.Internal.SigV4;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal.SigV4;

/// <summary>
/// Exercises the canonical request + string-to-sign against the
/// <c>get-vanilla</c> vector from the public AWS Signature V4 test suite.
/// Any change to the builder that breaks these strings will misalign us with
/// every real AWS client.
/// </summary>
public class SigV4CanonicalRequestBuilderTests
{
    // get-vanilla test vector — empty-body GET / on example.amazonaws.com.
    // https://docs.aws.amazon.com/general/latest/gr/sigv4-create-canonical-request.html
    private const string ExpectedCanonicalRequest =
        "GET\n" +
        "/\n" +
        "\n" +
        "host:example.amazonaws.com\n" +
        "x-amz-date:20150830T123600Z\n" +
        "\n" +
        "host;x-amz-date\n" +
        "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    private const string ExpectedStringToSign =
        "AWS4-HMAC-SHA256\n" +
        "20150830T123600Z\n" +
        "20150830/us-east-1/service/aws4_request\n" +
        "bb579772317eb040ac9ed261061d46c1f17a8133879d6129b6e1c25292927e63";

    [Fact]
    public void Builds_the_get_vanilla_canonical_request_byte_for_byte()
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = "example.amazonaws.com",
            ["x-amz-date"] = "20150830T123600Z",
        };

        string canonical = SigV4CanonicalRequestBuilder.BuildCanonicalRequest(
            method: "GET",
            canonicalUri: "/",
            canonicalQueryString: string.Empty,
            headers: headers,
            signedHeaders: ["host", "x-amz-date"],
            body: ReadOnlyMemory<byte>.Empty);

        canonical.ShouldBe(ExpectedCanonicalRequest);
    }

    [Fact]
    public void Builds_the_get_vanilla_string_to_sign_byte_for_byte()
    {
        SigV4Scope scope = new("20150830", "us-east-1", "service");

        string stringToSign = SigV4CanonicalRequestBuilder.BuildStringToSign(
            amzDate: "20150830T123600Z",
            scope: scope,
            canonicalRequest: ExpectedCanonicalRequest);

        stringToSign.ShouldBe(ExpectedStringToSign);
    }

    [Fact]
    public void HashPayload_of_empty_body_matches_the_well_known_SHA256()
    {
        SigV4CanonicalRequestBuilder.HashPayload(ReadOnlySpan<byte>.Empty)
            .ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact]
    public void HashPayload_of_hello_world_matches_the_well_known_SHA256()
    {
        byte[] body = Encoding.UTF8.GetBytes("hello world");
        SigV4CanonicalRequestBuilder.HashPayload(body)
            .ShouldBe("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9");
    }

    [Theory]
    [InlineData("  leading", "leading")]
    [InlineData("trailing  ", "trailing")]
    [InlineData("two   spaces", "two spaces")]
    [InlineData("  leading and trailing  ", "leading and trailing")]
    [InlineData("mixed\ttab \t spaces", "mixed tab spaces")]
    public void TrimAllWhitespace_collapses_internal_runs(string input, string expected)
    {
        SigV4CanonicalRequestBuilder.TrimAllWhitespace(input).ShouldBe(expected);
    }
}
