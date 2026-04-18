using Granit.Diagnostics;
using Granit.IoT.Abstractions;
using Granit.IoT.Abstractions.Internal;
using Granit.IoT.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.IoT.Extensions;

/// <summary>
/// Service-collection extensions for the root IoT abstractions module
/// (<c>Granit.IoT</c>).
/// </summary>
public static class IoTServiceCollectionExtensions
{
    /// <summary>
    /// Registers the IoT activity source, <c>IoTMetrics</c> singleton and
    /// the null GDPR locator. Idempotent via <c>TryAdd*</c>. Hosts that
    /// connect <c>Granit.Privacy</c> should register their own
    /// <c>IIoTDataSubjectLocator</c> after this call.
    /// </summary>
    public static IServiceCollection AddGranitIoT(this IServiceCollection services)
    {
        GranitActivitySourceRegistry.Register(IoTActivitySource.Name);
        services.TryAddSingleton<IoTMetrics>();

        // GDPR integration point: a host that connects Granit.Privacy must
        // register its own IIoTDataSubjectLocator that maps a user id to the
        // serial numbers of their devices. The null default ensures the
        // module does not claim personal data until an explicit mapping is
        // provided — see SECURITY.md "Trust boundaries" for guidance.
        services.TryAddSingleton<IIoTDataSubjectLocator, NullIoTDataSubjectLocator>();

        return services;
    }
}
