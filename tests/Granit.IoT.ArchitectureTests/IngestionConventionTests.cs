using ArchUnitNET.Domain;
using Granit.ArchitectureTests.Abstractions.Rules;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Validates conventions specific to the IoT ingestion pipeline:
/// integration events, signature validators, parsers, and Wolverine handlers.
/// </summary>
public sealed class IngestionConventionTests
{
    private const string TypePrefix = "Granit.IoT";
    private const string IntegrationEventInterface = "Granit.Events.IIntegrationEvent";
    private const string SignatureValidatorInterface = "Granit.IoT.Ingestion.Abstractions.IPayloadSignatureValidator";
    private const string MessageParserInterface = "Granit.IoT.Ingestion.Abstractions.IInboundMessageParser";

    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void Integration_events_should_have_Eto_suffix()
    {
        IReadOnlyList<Class> events = ImplementorsOf(IntegrationEventInterface)
            .Where(c => c.FullName.StartsWith(TypePrefix, StringComparison.Ordinal))
            .ToList();

        events.ShouldNotBeEmpty();

        IEnumerable<Class> violators = events
            .Where(c => !c.Name.EndsWith("Eto", StringComparison.Ordinal));

        violators.ShouldBeEmpty(
            "Integration events under Granit.IoT must end with 'Eto'. " +
            $"Violators: {string.Join(", ", violators.Select(c => c.FullName))}");
    }

    [Fact]
    public void Integration_events_should_be_sealed()
    {
        IEnumerable<Class> unsealed = ImplementorsOf(IntegrationEventInterface)
            .Where(c => c.FullName.StartsWith(TypePrefix, StringComparison.Ordinal))
            .Where(c => c.IsSealed != true);

        unsealed.ShouldBeEmpty(
            "Integration events should be sealed records. " +
            $"Violators: {string.Join(", ", unsealed.Select(c => c.FullName))}");
    }

    [Fact]
    public void Signature_validators_should_be_internal()
    {
        IEnumerable<Class> publicValidators = ImplementorsOf(SignatureValidatorInterface)
            .Where(c => c.FullName.StartsWith(TypePrefix, StringComparison.Ordinal))
            .Where(c => c.Visibility == Visibility.Public);

        publicValidators.ShouldBeEmpty(
            "IPayloadSignatureValidator implementations should be internal. " +
            $"Violators: {string.Join(", ", publicValidators.Select(c => c.FullName))}");
    }

    [Fact]
    public void Message_parsers_should_be_internal()
    {
        IEnumerable<Class> publicParsers = ImplementorsOf(MessageParserInterface)
            .Where(c => c.FullName.StartsWith(TypePrefix, StringComparison.Ordinal))
            .Where(c => c.Visibility == Visibility.Public);

        publicParsers.ShouldBeEmpty(
            "IInboundMessageParser implementations should be internal. " +
            $"Violators: {string.Join(", ", publicParsers.Select(c => c.FullName))}");
    }

    [Fact]
    public void IngestionPipeline_should_be_sealed_and_internal()
    {
        Class? pipeline = Architecture.Classes
            .FirstOrDefault(c => c.FullName == "Granit.IoT.Ingestion.Internal.IngestionPipeline");

        pipeline.ShouldNotBeNull("IngestionPipeline must exist in Granit.IoT.Ingestion.Internal.");
        pipeline.IsSealed.ShouldNotBeNull().ShouldBeTrue();
        pipeline.Visibility.ShouldNotBe(Visibility.Public);
    }

    [Fact]
    public void Options_classes_should_be_sealed() =>
        ClassDesignRules.OptionsClassesShouldBeSealed(Architecture, TypePrefix);

    [Fact]
    public void Exception_classes_should_end_with_Exception() =>
        NamingConventionRules.ExceptionClassesShouldEndWithException(Architecture, TypePrefix);

    private static IEnumerable<Class> ImplementorsOf(string interfaceFullName) =>
        Architecture.Classes
            .Where(c => c.ImplementedInterfaces.Any(i => i.FullName == interfaceFullName));
}
