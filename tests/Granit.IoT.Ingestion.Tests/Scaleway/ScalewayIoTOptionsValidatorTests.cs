using Granit.IoT.Ingestion.Scaleway.Internal;
using Granit.IoT.Ingestion.Scaleway.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Scaleway;

public sealed class ScalewayIoTOptionsValidatorTests
{
    [Fact]
    public void Validate_NullOptions_Throws()
    {
        ScalewayIoTOptionsValidator validator = new(StubEnv("Production"));

        Should.Throw<ArgumentNullException>(() => validator.Validate(null, null!));
    }

    [Fact]
    public void Validate_NoSharedSecret_AlwaysPasses()
    {
        ScalewayIoTOptionsValidator validator = new(StubEnv("Production"));
        ScalewayIoTOptions opts = new() { SharedSecret = string.Empty };

        ValidateOptionsResult result = validator.Validate(null, opts);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_SharedSecretInDevelopment_Allowed()
    {
        ScalewayIoTOptionsValidator validator = new(StubEnv("Development"));
        ScalewayIoTOptions opts = new() { SharedSecret = "shh" };

        ValidateOptionsResult result = validator.Validate(null, opts);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_SharedSecretInProduction_Fails()
    {
        ScalewayIoTOptionsValidator validator = new(StubEnv("Production"));
        ScalewayIoTOptions opts = new() { SharedSecret = "leaked" };

        ValidateOptionsResult result = validator.Validate(null, opts);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Granit.Vault");
    }

    private static IHostEnvironment StubEnv(string name)
    {
        IHostEnvironment env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(name);
        return env;
    }
}
