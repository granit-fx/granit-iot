using Granit.IoT.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Extensions;

/// <summary>
/// EF Core <see cref="ModelBuilder"/> extensions that apply the core IoT
/// entity configurations (<c>Device</c>, <c>TelemetryPoint</c>).
/// </summary>
public static class IoTModelBuilderExtensions
{
    /// <summary>
    /// Applies the IoT module entity configurations to <paramref name="modelBuilder"/>.
    /// </summary>
    /// <returns>The same <see cref="ModelBuilder"/>, for chaining.</returns>
    public static ModelBuilder ConfigureIoTModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DeviceConfiguration());
        modelBuilder.ApplyConfiguration(new TelemetryPointConfiguration());
        return modelBuilder;
    }
}
