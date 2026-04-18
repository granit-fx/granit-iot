using Granit.IoT.Ingestion.Aws;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests;

public sealed class SnsSigningCertFetchExceptionTests
{
    [Fact]
    public void DefaultCtor_Works()
    {
        SnsSigningCertFetchException ex = new();
        ex.ShouldNotBeNull();
    }

    [Fact]
    public void MessageCtor_PreservesMessage()
    {
        SnsSigningCertFetchException ex = new("boom");
        ex.Message.ShouldBe("boom");
    }

    [Fact]
    public void InnerCtor_PreservesInner()
    {
        InvalidOperationException inner = new("inner");
        SnsSigningCertFetchException ex = new("boom", inner);
        ex.Message.ShouldBe("boom");
        ex.InnerException.ShouldBeSameAs(inner);
    }
}
