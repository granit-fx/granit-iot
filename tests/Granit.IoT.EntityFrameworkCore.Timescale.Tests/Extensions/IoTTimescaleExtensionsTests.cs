using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Timescale.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Timescale.Tests.Extensions;

public sealed class IoTTimescaleExtensionsTests
{
    [Fact]
    public void EnableTelemetryHypertable_NullBuilder_Throws()
    {
        MigrationBuilder? mb = null;
        Should.Throw<ArgumentNullException>(() => mb!.EnableTelemetryHypertable());
    }

    [Fact]
    public void EnableTelemetryHypertable_AppendsSql()
    {
        MigrationBuilder mb = new("Npgsql");
        mb.EnableTelemetryHypertable();
        mb.Operations.Count.ShouldBe(1);
    }

    [Fact]
    public void CreateTelemetryHourlyAggregate_AppendsTwoOperations()
    {
        MigrationBuilder mb = new("Npgsql");
        mb.CreateTelemetryHourlyAggregate();
        mb.Operations.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateTelemetryDailyAggregate_AppendsTwoOperations()
    {
        MigrationBuilder mb = new("Npgsql");
        mb.CreateTelemetryDailyAggregate();
        mb.Operations.Count.ShouldBe(2);
    }

    [Fact]
    public void AddGranitIoTTimescale_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTTimescale());
    }

    [Fact]
    public void AddGranitIoTTimescale_RegistersTimescaleReader()
    {
        ServiceCollection services = new();
        services.AddGranitIoTTimescale();

        services.ShouldContain(d => d.ServiceType == typeof(ITelemetryReader));
    }
}
