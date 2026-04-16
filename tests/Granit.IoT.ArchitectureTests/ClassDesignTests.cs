using Granit.ArchitectureTests.Abstractions.Rules;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Validates class design conventions: sealed DbContexts, internal EfStore implementations,
/// no public types in Internal namespaces, entity configuration confinement.
/// </summary>
public sealed class ClassDesignTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void DbContext_classes_should_be_sealed() =>
        ClassDesignRules.DbContextClassesShouldBeSealed(Architecture);

    [Fact]
    public void No_MVC_controllers_allowed() =>
        Should.NotThrow(() => ClassDesignRules.NoMvcControllersAllowed(Architecture, "Granit.IoT"));

    [Fact]
    public void EntityTypeConfigurations_should_be_in_EfCore_layer() =>
        ClassDesignRules.EntityTypeConfigurationsShouldBeInEfCoreLayer(Architecture, "Granit.IoT");

    [Fact]
    public void Entity_configurations_should_not_be_public() =>
        ClassDesignRules.EntityConfigurationsShouldNotBePublic(Architecture, "Granit.IoT");

    [Fact]
    public void Public_types_should_not_reside_in_Internal_namespaces() =>
        ClassDesignRules.PublicTypesShouldNotResideInInternalNamespaces(Architecture, "Granit.IoT");

    [Fact]
    public void Concrete_exception_classes_should_be_sealed() =>
        ClassDesignRules.ConcreteExceptionClassesShouldBeSealed(Architecture, "Granit.IoT");
}
