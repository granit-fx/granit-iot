using Granit.ArchitectureTests.Abstractions;

namespace Granit.IoT.ArchitectureTests;

/// <summary>
/// Loads the entire Granit.IoT architecture graph once per test run.
/// All test classes share this static instance to avoid repeated assembly scanning.
/// </summary>
internal static class IoTArchitecture
{
    internal static readonly ArchUnitNET.Domain.Architecture Instance =
        ArchitectureLoader.Load("Granit.IoT", typeof(IoTArchitecture).Assembly);
}
