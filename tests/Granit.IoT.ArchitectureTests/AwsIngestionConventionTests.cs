using ArchUnitNET.Domain;
using Shouldly;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Conventions for <c>Granit.IoT.Ingestion.Aws</c>: validators and parsers must
/// be internal (the public API is the module + DI extension), exactly one
/// <c>GranitModule</c> at the root, and any <c>SignatureValidator</c> must
/// implement <see cref="Granit.IoT.Ingestion.Abstractions.IPayloadSignatureValidator"/>.
/// </summary>
public sealed class AwsIngestionConventionTests
{
    private const string AwsNamespacePrefix = "Granit.IoT.Ingestion.Aws";
    private const string InternalNamespacePrefix = "Granit.IoT.Ingestion.Aws.Internal";
    private const string SigV4NamespacePrefix = "Granit.IoT.Ingestion.Aws.Internal.SigV4";
    private const string ValidatorInterface = "Granit.IoT.Ingestion.Abstractions.IPayloadSignatureValidator";

    private static readonly ArchUnitNET.Domain.Architecture Architecture = IoTArchitecture.Instance;

    [Fact]
    public void Module_should_live_at_the_root_of_the_namespace()
    {
        var modules = Architecture.Classes
            .Where(c => c.FullName.StartsWith(AwsNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("Module", StringComparison.Ordinal))
            .ToList();

        modules.Count.ShouldBe(1);
        modules[0].FullName.ShouldBe("Granit.IoT.Ingestion.Aws.GranitIoTIngestionAwsModule");
    }

    [Fact]
    public void Internal_implementations_must_not_be_public()
    {
        IEnumerable<Class> publicTypes = Architecture.Classes
            .Where(c => c.FullName.StartsWith(InternalNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Visibility == Visibility.Public);

        publicTypes.ShouldBeEmpty(
            "Types under Granit.IoT.Ingestion.Aws.Internal must be internal. " +
            $"Violators: {string.Join(", ", publicTypes.Select(c => c.FullName))}");
    }

    [Fact]
    public void SigV4_helper_types_stay_under_Internal_SigV4_namespace()
    {
        // Crypto primitives (canonical request, signing key derivation) must
        // not leak into the public surface — only ISigV4RequestValidator and
        // ISigV4SigningKeyProvider are intended consumer contracts.
        IEnumerable<Class> publicTypes = Architecture.Classes
            .Where(c => c.FullName.StartsWith(SigV4NamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Visibility == Visibility.Public);

        publicTypes.ShouldBeEmpty(
            "SigV4 internals (canonical request builder, signing key derivation) must remain internal. " +
            $"Violators: {string.Join(", ", publicTypes.Select(c => c.FullName))}");
    }

    [Fact]
    public void Signature_validators_must_implement_IPayloadSignatureValidator()
    {
        IReadOnlyList<Class> validators = Architecture.Classes
            .Where(c => c.FullName.StartsWith(InternalNamespacePrefix, StringComparison.Ordinal))
            .Where(c => c.Name.EndsWith("SignatureValidator", StringComparison.Ordinal))
            .ToList();

        validators.ShouldNotBeEmpty(
            "Expected at least one *SignatureValidator under Granit.IoT.Ingestion.Aws.Internal.");

        IEnumerable<Class> notImplementing = validators
            .Where(c => !c.ImplementedInterfaces.Any(i => i.FullName == ValidatorInterface));

        notImplementing.ShouldBeEmpty(
            $"Every *SignatureValidator must implement {ValidatorInterface}. " +
            $"Violators: {string.Join(", ", notImplementing.Select(c => c.FullName))}");
    }
}
