using Granit.IoT.Wolverine.Abstractions;
using Granit.IoT.Wolverine.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Ingestion.Tests.Wolverine;

public sealed class WolverineServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTWolverine_NullServices_Throws()
    {
        IServiceCollection? services = null;

        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTWolverine());
    }

    [Fact]
    public void AddGranitIoTWolverine_RegistersThresholdEvaluator()
    {
        ServiceCollection services = new();

        services.AddGranitIoTWolverine();

        services.ShouldContain(d => d.ServiceType == typeof(IDeviceThresholdEvaluator));
    }

    [Fact]
    public void AddGranitIoTWolverine_IsIdempotent()
    {
        ServiceCollection services = new();
        services.AddGranitIoTWolverine();
        services.AddGranitIoTWolverine();

        int count = services.Count(d => d.ServiceType == typeof(IDeviceThresholdEvaluator));
        count.ShouldBe(1);
    }
}
