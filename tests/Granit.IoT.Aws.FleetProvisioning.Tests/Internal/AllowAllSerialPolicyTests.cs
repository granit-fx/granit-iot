using Granit.IoT.Aws.FleetProvisioning.Abstractions;
using Granit.IoT.Aws.FleetProvisioning.Internal;
using Shouldly;

namespace Granit.IoT.Aws.FleetProvisioning.Tests.Internal;

public sealed class AllowAllSerialPolicyTests
{
    [Fact]
    public async Task EvaluateAsync_AlwaysAllows()
    {
        AllowAllSerialPolicy policy = new();

        SerialPolicyDecision decision = await policy.EvaluateAsync(
            "anything",
            tenantId: Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        decision.Allowed.ShouldBeTrue();
        decision.DenyReason.ShouldBeNull();
    }

    [Fact]
    public void SerialPolicyDecision_Allow_IsTrue()
    {
        SerialPolicyDecision decision = SerialPolicyDecision.Allow;

        decision.Allowed.ShouldBeTrue();
        decision.DenyReason.ShouldBeNull();
    }

    [Fact]
    public void SerialPolicyDecision_Deny_IsFalseWithReason()
    {
        var decision = SerialPolicyDecision.Deny("nope");

        decision.Allowed.ShouldBeFalse();
        decision.DenyReason.ShouldBe("nope");
    }
}
