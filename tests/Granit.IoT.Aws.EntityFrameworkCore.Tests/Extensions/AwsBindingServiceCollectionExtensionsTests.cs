using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Aws.EntityFrameworkCore.Tests.Extensions;

public sealed class AwsBindingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTAwsEntityFrameworkCore_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTAwsEntityFrameworkCore(_ => { }));
    }

    [Fact]
    public void AddGranitIoTAwsEntityFrameworkCore_NullConfigure_Throws()
    {
        ServiceCollection services = new();
        Should.Throw<ArgumentNullException>(() => services.AddGranitIoTAwsEntityFrameworkCore(null!));
    }

    [Fact]
    public void AddGranitIoTAwsEntityFrameworkCore_RegistersReadersAndWriters()
    {
        ServiceCollection services = new();

        services.AddGranitIoTAwsEntityFrameworkCore(o => o.UseSqlite("DataSource=:memory:"));

        services.ShouldContain(d => d.ServiceType == typeof(IAwsThingBindingReader));
        services.ShouldContain(d => d.ServiceType == typeof(IAwsThingBindingWriter));
    }
}
