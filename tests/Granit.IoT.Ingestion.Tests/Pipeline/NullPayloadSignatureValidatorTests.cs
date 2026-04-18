using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Internal;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Pipeline;

public sealed class NullPayloadSignatureValidatorTests
{
    [Fact]
    public async Task ValidateAsync_AlwaysReturnsValid()
    {
        NullPayloadSignatureValidator validator = new();

        SignatureValidationResult result = await validator.ValidateAsync(
            ReadOnlyMemory<byte>.Empty,
            new Dictionary<string, string>(),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SourceName_IsDevelopment()
    {
        NullPayloadSignatureValidator validator = new();

        validator.SourceName.ShouldBe(NullPayloadSignatureValidator.DevelopmentSourceName);
        NullPayloadSignatureValidator.DevelopmentSourceName.ShouldBe("development");
    }
}
