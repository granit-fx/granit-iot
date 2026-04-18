using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.EntityFrameworkCore.Internal;
using Granit.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Aws.EntityFrameworkCore.Extensions;

/// <summary>
/// Service-collection extensions for the AWS bridge persistence layer
/// (<c>Granit.IoT.Aws.EntityFrameworkCore</c>).
/// </summary>
public static class AwsBindingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AWS bridge persistence layer: an isolated
    /// <see cref="IDbContextFactory{AwsBindingDbContext}"/> plus the EF Core
    /// implementations of <see cref="IAwsThingBindingReader"/> and
    /// <see cref="IAwsThingBindingWriter"/>.
    /// </summary>
    public static IServiceCollection AddGranitIoTAwsEntityFrameworkCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddGranitDbContext<AwsBindingDbContext>(configure);

        services.TryAddScoped<IAwsThingBindingReader, AwsThingBindingEfCoreReader>();
        services.TryAddScoped<IAwsThingBindingWriter, AwsThingBindingEfCoreWriter>();

        return services;
    }
}
