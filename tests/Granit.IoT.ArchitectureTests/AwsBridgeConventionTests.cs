using ArchUnitNET.Domain;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Conventions for the <c>Granit.IoT.Aws</c> bridge: a single
/// <c>GranitModule</c> at the namespace root and no public types under
/// <c>Internal</c>. Aggregate-level rules (no public setters, factory method,
/// private parameterless constructor) are already enforced for the whole
/// <c>Granit.IoT</c> prefix by <see cref="DomainConventionTests"/>.
/// </summary>
public sealed class AwsBridgeConventionTests
{
    private const string AwsNamespacePrefix = "Granit.IoT.Aws";
    private const string InternalNamespacePrefix = "Granit.IoT.Aws.Internal";

    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void Module_should_live_at_the_root_of_the_namespace()
    {
        var modules = Architecture.Classes
            .Where(c => c.FullName.StartsWith(AwsNamespacePrefix, StringComparison.Ordinal))
            .Where(c => !c.FullName.StartsWith("Granit.IoT.Aws.Tests", StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("Module", StringComparison.Ordinal))
            .ToList();

        modules.Count.ShouldBe(1);
        modules[0].FullName.ShouldBe("Granit.IoT.Aws.GranitIoTAwsModule");
    }

    [Fact]
    public void Internal_implementations_must_not_be_public()
    {
        IEnumerable<Class> publicTypes = Architecture.Classes
            .Where(c => c.FullName.StartsWith(InternalNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Visibility == Visibility.Public);

        publicTypes.ShouldBeEmpty(
            "Types under Granit.IoT.Aws.Internal must be internal. " +
            $"Violators: {string.Join(", ", publicTypes.Select(c => c.FullName))}");
    }
}
