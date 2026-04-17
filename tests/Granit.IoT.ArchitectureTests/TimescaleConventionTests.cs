using ArchUnitNET.Domain;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Conventions for <c>Granit.IoT.EntityFrameworkCore.Timescale</c>: the module
/// must live at the root of the package, implementations that touch raw SQL
/// must be internal, and the public surface is limited to extension methods
/// plus the module class.
/// </summary>
public sealed class TimescaleConventionTests
{
    private const string TimescaleNamespacePrefix = "Granit.IoT.EntityFrameworkCore.Timescale";
    private const string InternalNamespacePrefix = "Granit.IoT.EntityFrameworkCore.Timescale.Internal";

    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void Module_should_live_at_the_root_of_the_Timescale_namespace()
    {
        var modules = Architecture.Classes
            .Where(c => c.FullName.StartsWith(TimescaleNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("Module", StringComparison.Ordinal))
            .ToList();

        modules.Count.ShouldBe(1,
            "Granit.IoT.EntityFrameworkCore.Timescale must expose exactly one GranitModule.");
        modules[0].FullName.ShouldBe(
            "Granit.IoT.EntityFrameworkCore.Timescale.GranitIoTTimescaleModule");
    }

    [Fact]
    public void Sql_builders_and_readers_should_be_internal()
    {
        IEnumerable<Class> publicTypes = Architecture.Classes
            .Where(c => c.FullName.StartsWith(InternalNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Visibility == Visibility.Public);

        publicTypes.ShouldBeEmpty(
            "Types under Granit.IoT.EntityFrameworkCore.Timescale.Internal must be internal. " +
            $"Violators: {string.Join(", ", publicTypes.Select(c => c.FullName))}");
    }

    [Fact]
    public void Timescale_reader_must_derive_from_Postgres_reader()
    {
        Class? reader = Architecture.Classes.SingleOrDefault(c =>
            c.FullName == $"{InternalNamespacePrefix}.TimescaleTelemetryEfCoreReader");

        reader.ShouldNotBeNull(
            "TimescaleTelemetryEfCoreReader is missing — the Timescale package must implement a reader.");

        reader!.BaseClass?.FullName.ShouldBe(
            "Granit.IoT.EntityFrameworkCore.Postgres.Internal.PostgresTelemetryEfCoreReader",
            "The Timescale reader must extend the Postgres reader to reuse JSONB aggregation for sub-hourly windows.");
    }
}
