using Granit.IoT.Ingestion.Aws.Internal.SigV4;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal.SigV4;

public class SigV4ScopeTests
{
    [Fact]
    public void ToString_returns_canonical_slash_separated_form()
    {
        SigV4Scope scope = new("20150830", "us-east-1", "service");
        scope.ToString().ShouldBe("20150830/us-east-1/service/aws4_request");
    }

    [Theory]
    [InlineData("20150830/us-east-1/service/aws4_request", "20150830", "us-east-1", "service")]
    [InlineData("20260417/eu-west-3/iotdata/aws4_request", "20260417", "eu-west-3", "iotdata")]
    public void TryParse_accepts_well_formed_scopes(string input, string date, string region, string service)
    {
        SigV4Scope.TryParse(input, out SigV4Scope scope).ShouldBeTrue();
        scope.ShouldBe(new SigV4Scope(date, region, service));
    }

    [Theory]
    [InlineData("")]
    [InlineData("20150830/us-east-1/service")]                   // missing terminator
    [InlineData("20150830/us-east-1/service/wrong")]             // wrong terminator
    [InlineData("20150830//service/aws4_request")]               // missing region
    [InlineData("20150830/us-east-1//aws4_request")]             // missing service
    [InlineData("2015-08-30/us-east-1/service/aws4_request")]    // wrong date format
    [InlineData("2015083X/us-east-1/service/aws4_request")]      // non-numeric date
    public void TryParse_rejects_malformed_scopes(string input)
    {
        SigV4Scope.TryParse(input, out _).ShouldBeFalse();
    }
}
