using Granit.ArchitectureTests.Abstractions.Rules;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Validates naming conventions: interface prefix, CQRS Reader/Writer, no Dto suffix.
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
}
