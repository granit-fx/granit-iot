using ArchUnitNET.Domain;
using Granit.ArchitectureTests.Abstractions.Rules;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Validates naming conventions: interface prefix, CQRS Reader/Writer, no Dto suffix,
/// and module-name homogeneity on metrics classes (the canonical module name
/// <c>IoT</c> must come before any provider / variant token).
/// </summary>
public sealed class NamingConventionTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void Interfaces_should_start_with_I() =>
        NamingConventionRules.InterfacesShouldStartWithI(Architecture, "Granit.IoT");

    [Fact]
    public void Reader_interfaces_should_end_with_Reader() =>
        NamingConventionRules.ReaderInterfacesShouldEndWithReader(Architecture);

    [Fact]
    public void Writer_interfaces_should_end_with_Writer() =>
        NamingConventionRules.WriterInterfacesShouldEndWithWriter(Architecture);

    [Fact]
    public void Endpoint_types_should_not_use_Dto_suffix() =>
        NamingConventionRules.EndpointTypesShouldNotUseDtoSuffix(Architecture, "Granit.IoT");

    /// <summary>
    /// CLAUDE.md §3e: every <c>*Metrics</c> class must start with <c>IoT</c> so that
    /// <c>IoT</c>-scoped dashboards pick them up consistently. Historical drift
    /// (<c>AwsIoTIngestionMetrics</c> — with <c>IoT</c> in the middle) is rejected;
    /// the correct form is <c>IoTIngestionAwsMetrics</c>. Applies across all
    /// namespaces including <c>.Diagnostics</c> and <c>.Internal</c>.
    /// </summary>
    [Fact]
    public void Metrics_classes_must_start_with_IoT()
    {
        IEnumerable<Class> violators = Architecture.Classes
            .Where(c => c.FullName.StartsWith("Granit.IoT", StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("Metrics", StringComparison.Ordinal))
            .Where(c => !c.Name.StartsWith("IoT", StringComparison.Ordinal));

        violators.ShouldBeEmpty(
            "Metrics classes must start with 'IoT' for module-name homogeneity. " +
            "Rename e.g. 'AwsIoTIngestionMetrics' → 'IoTIngestionAwsMetrics'. " +
            $"Violators: {string.Join(", ", violators.Select(c => c.FullName))}");
    }

    /// <summary>
    /// Metrics classes must not use the <c>BridgeMetrics</c> suffix — it is ambiguous
    /// (every satellite is a bridge of some sort). The convention is
    /// <c>IoT{Satellite}Metrics</c>, matching the satellite project name (e.g.
    /// <c>IoTMetrics</c>, <c>IoTMqttMetrics</c>, <c>IoTIngestionAwsMetrics</c>).
    /// </summary>
    [Fact]
    public void Metrics_classes_must_not_use_Bridge_suffix()
    {
        IEnumerable<Class> violators = Architecture.Classes
            .Where(c => c.FullName.StartsWith("Granit.IoT", StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("BridgeMetrics", StringComparison.Ordinal));

        violators.ShouldBeEmpty(
            "Metrics classes must not use the 'BridgeMetrics' suffix — rename to " +
            "'IoT{Satellite}Metrics' for consistency with other satellites. " +
            $"Violators: {string.Join(", ", violators.Select(c => c.FullName))}");
    }
}
