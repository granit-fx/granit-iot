using System.Diagnostics.Metrics;

namespace Granit.IoT.Ingestion.Tests;

internal sealed class EmptyMeterFactory : IMeterFactory
{
    public Meter Create(MeterOptions options) => new(options);

    public void Dispose() { }
}
