using Granit.DataFiltering;
using Granit.IoT.Aws.Domain;
using Granit.IoT.Aws.EntityFrameworkCore.Extensions;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.Aws.EntityFrameworkCore.Internal;

internal sealed class AwsBindingDbContext(
    DbContextOptions<AwsBindingDbContext> options,
    ICurrentTenant? currentTenant = null,
    IDataFilter? dataFilter = null)
    : DbContext(options)
{
    public DbSet<AwsThingBinding> ThingBindings => Set<AwsThingBinding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureGranitIoTAws();
        modelBuilder.ApplyGranitConventions(currentTenant, dataFilter);
    }
}
