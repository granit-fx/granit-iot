using Granit.DataFiltering;
using Granit.IoT.Domain;
using Granit.IoT.EntityFrameworkCore.Extensions;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore;
using Granit.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Internal;

internal sealed class IoTDbContext(
    DbContextOptions<IoTDbContext> options,
    ICurrentTenant? currentTenant = null,
    IDataFilter? dataFilter = null)
    : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();

    public DbSet<TelemetryPoint> TelemetryPoints => Set<TelemetryPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureIoTModule();
        modelBuilder.ApplyGranitConventions(currentTenant, dataFilter);
    }
}
