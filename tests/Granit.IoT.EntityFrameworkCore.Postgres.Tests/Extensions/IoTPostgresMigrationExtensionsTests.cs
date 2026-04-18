using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Postgres.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Tests.Extensions;

public sealed class IoTPostgresMigrationExtensionsTests
{
    [Fact]
    public void CreateTelemetryBrinIndex_AppendsSqlOperation()
    {
        MigrationBuilder builder = new("Npgsql");

        builder.CreateTelemetryBrinIndex();

        builder.Operations.Count.ShouldBe(1);
        builder.Operations.OfType<SqlOperation>().ShouldNotBeEmpty();
    }

    [Fact]
    public void CreateTelemetryGinIndex_AppendsSqlOperation()
    {
        MigrationBuilder builder = new("Npgsql");

        builder.CreateTelemetryGinIndex();

        builder.Operations.Count.ShouldBe(1);
    }

    [Fact]
    public void CreateIoTPostgresIndexes_AppendsBothIndexes()
    {
        MigrationBuilder builder = new("Npgsql");

        builder.CreateIoTPostgresIndexes(schema: "iot");

        builder.Operations.Count.ShouldBe(2);
    }

    [Fact]
    public void EnableTelemetryPartitioning_AppendsSql()
    {
        MigrationBuilder builder = new("Npgsql");

        builder.EnableTelemetryPartitioning();

        builder.Operations.Count.ShouldBe(1);
    }

    [Fact]
    public void CreateTelemetryPartition_AppendsSql()
    {
        MigrationBuilder builder = new("Npgsql");

        builder.CreateTelemetryPartition(2026, 4);

        builder.Operations.Count.ShouldBe(1);
    }

    [Fact]
    public void AddGranitIoTPostgres_NullServices_Throws()
    {
        IServiceCollection? services = null;

        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTPostgres());
    }

    [Fact]
    public void AddGranitIoTPostgres_ReplacesPartitionMaintainerAndTelemetryReader()
    {
        ServiceCollection services = new();

        services.AddGranitIoTPostgres();

        services.ShouldContain(d => d.ServiceType == typeof(ITelemetryPartitionMaintainer));
        services.ShouldContain(d => d.ServiceType == typeof(ITelemetryReader));
    }
}
