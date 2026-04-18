using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.EntityFrameworkCore.Tests;

public sealed class IoTEntityFrameworkCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTEntityFrameworkCore_RegistersAllReadersAndWriters()
    {
        ServiceCollection services = new();

        services.AddGranitIoTEntityFrameworkCore(o => o.UseSqlite("DataSource=:memory:"));

        services.ShouldContain(d => d.ServiceType == typeof(IDeviceReader));
        services.ShouldContain(d => d.ServiceType == typeof(IDeviceWriter));
        services.ShouldContain(d => d.ServiceType == typeof(IDeviceLookup));
        services.ShouldContain(d => d.ServiceType == typeof(ITelemetryReader));
        services.ShouldContain(d => d.ServiceType == typeof(ITelemetryWriter));
        services.ShouldContain(d => d.ServiceType == typeof(ITelemetryPurger));
    }
}
