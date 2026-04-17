using Granit.IoT.Aws.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.Aws.EntityFrameworkCore.Extensions;

public static class AwsBindingModelBuilderExtensions
{
    public static ModelBuilder ConfigureGranitIoTAws(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AwsThingBindingConfiguration());
        return modelBuilder;
    }
}
