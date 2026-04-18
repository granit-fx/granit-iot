using System.Diagnostics.Metrics;
using Granit.IoT.Mqtt.Mqttnet.Internal;
using Shouldly;

namespace Granit.IoT.Mqtt.Mqttnet.Tests.Internal;

public sealed class IoTMqttMetricsTests
{
    [Fact]
    public void Record_AllCounters_DoesNotThrow()
    {
        IoTMqttMetrics metrics = new(new TestMeterFactory());

        Should.NotThrow(() =>
        {
            metrics.RecordReceived();
            metrics.RecordDispatched("Accepted");
            metrics.RecordFeatureDisabled();
            metrics.RecordConnectionFailure();
            metrics.RecordReconnectAttempt();
            metrics.RecordCertificateReload();
        });
    }

    [Fact]
    public void Counters_AreEmittedOnTheMqttMeter()
    {
        TestMeterFactory factory = new();
        IoTMqttMetrics metrics = new(factory);
        using MeterListener listener = new();

        long total = 0;
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == IoTMqttMetrics.MeterName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => Interlocked.Add(ref total, value));
        listener.Start();

        metrics.RecordReceived();
        metrics.RecordDispatched("ok");

        total.ShouldBe(2);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
