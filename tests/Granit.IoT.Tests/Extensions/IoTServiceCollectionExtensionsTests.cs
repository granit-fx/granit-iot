using Granit.IoT.Diagnostics;
using Granit.IoT.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Tests.Extensions;

public sealed class IoTServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoT_RegistersIoTMetrics()
    {
        ServiceCollection services = new();
        services.AddSingleton<System.Diagnostics.Metrics.IMeterFactory, EmptyMeterFactory>();

        services.AddGranitIoT();

        services.ShouldContain(d => d.ServiceType == typeof(IoTMetrics));
    }

    private sealed class EmptyMeterFactory : System.Diagnostics.Metrics.IMeterFactory
    {
        public System.Diagnostics.Metrics.Meter Create(System.Diagnostics.Metrics.MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
