using Granit.IoT.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Extensions;

public static class IoTModelBuilderExtensions
{
    public static ModelBuilder ConfigureIoTModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DeviceConfiguration());
        modelBuilder.ApplyConfiguration(new TelemetryPointConfiguration());
        return modelBuilder;
    }
}
