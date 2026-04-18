using Granit.IoT.Aws.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.Aws.EntityFrameworkCore.Extensions;

/// <summary>
/// EF Core <see cref="ModelBuilder"/> extensions that apply the AWS bridge
/// entity configurations (<c>AwsThingBinding</c>) to the host's DbContext.
/// </summary>
public static class AwsBindingModelBuilderExtensions
{
    /// <summary>
    /// Applies the AWS bridge entity configurations to <paramref name="modelBuilder"/>.
    /// </summary>
    /// <returns>The same <see cref="ModelBuilder"/>, for chaining.</returns>
    public static ModelBuilder ConfigureGranitIoTAws(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AwsThingBindingConfiguration());
        return modelBuilder;
    }
}
