using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Mqtt.Internal;
using Shouldly;

namespace Granit.IoT.Mqtt.Tests.Validation;

public sealed class MqttPayloadSignatureValidatorTests
{
    [Fact]
    public void SourceName_IsMqtt() =>
        new MqttPayloadSignatureValidator().SourceName.ShouldBe("mqtt");

    [Fact]
    public async Task ValidateAsync_AlwaysReturnsValid()
    {
        MqttPayloadSignatureValidator validator = new();
        IReadOnlyDictionary<string, string> headers = new Dictionary<string, string>();

        SignatureValidationResult result = await validator.ValidateAsync(
            new ReadOnlyMemory<byte>([1, 2, 3]),
            headers,
            TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
        result.FailureReason.ShouldBeNull();
    }
}
