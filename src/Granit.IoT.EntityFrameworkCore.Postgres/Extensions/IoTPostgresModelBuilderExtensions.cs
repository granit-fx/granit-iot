using Granit.IoT.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Extensions;

/// <summary>
/// Applies PostgreSQL-specific optimizations to the IoT model:
/// jsonb column type for <see cref="TelemetryPoint.Metrics"/> and
/// <see cref="Device.Tags"/>.
/// </summary>
/// <remarks>
/// Call this method AFTER <c>ConfigureIoTModule()</c> and BEFORE
/// <c>ApplyGranitConventions()</c> in your <c>OnModelCreating</c>.
/// BRIN and GIN indexes are created via raw SQL migration — see
/// <see cref="IoTPostgresMigrationExtensions"/>.
/// </remarks>
public static class IoTPostgresModelBuilderExtensions
{
    /// <summary>
    /// Applies PostgreSQL column-type tweaks (<c>jsonb</c> for <c>Metrics</c>
    /// and <c>Tags</c>) to the IoT model.
    /// </summary>
    /// <returns>The same <see cref="ModelBuilder"/>, for chaining.</returns>
    public static ModelBuilder ApplyIoTPostgresOptimizations(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryPoint>(builder =>
        {
            builder.Property(x => x.Metrics)
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<Device>(builder =>
        {
            builder.Property(x => x.Tags)
                .HasColumnType("jsonb");
        });

        return modelBuilder;
    }
}
