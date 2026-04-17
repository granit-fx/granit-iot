using Granit.IoT;
using Granit.IoT.BackgroundJobs;
using Granit.IoT.Endpoints;
using Granit.IoT.EntityFrameworkCore;
using Granit.IoT.Ingestion;
using Granit.IoT.Ingestion.Endpoints;
using Granit.IoT.Ingestion.Scaleway;
using Granit.IoT.Mcp;
using Granit.IoT.Notifications;
using Granit.IoT.Timeline;
using Granit.IoT.Wolverine;
using Granit.Modularity;

namespace Granit.Bundle.IoT;

/// <summary>
/// Extension methods on <see cref="GranitBuilder"/> for adding the Phase-1 IoT bundle.
/// </summary>
public static class GranitBuilderIoTExtensions
{
    /// <summary>
    /// Adds the Phase-1 IoT bundle: domain, persistence (PostgreSQL), endpoints,
    /// ingestion pipeline (+ Scaleway provider), Wolverine handlers, and the
    /// notifications bridge. Modules are appended in topological order; each
    /// module's own <c>[DependsOn]</c> graph still drives DI registration order.
    /// </summary>
    public static GranitBuilder AddIoT(this GranitBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Granit.IoT.EntityFrameworkCore.Postgres ships only model-builder + migration
        // extension methods (called explicitly by the host); it has no GranitModule class.
        builder.AddModule<GranitIoTModule>();
        builder.AddModule<GranitIoTEntityFrameworkCoreModule>();
        builder.AddModule<GranitIoTEndpointsModule>();
        builder.AddModule<GranitIoTIngestionModule>();
        builder.AddModule<GranitIoTIngestionEndpointsModule>();
        builder.AddModule<GranitIoTIngestionScalewayModule>();
        builder.AddModule<GranitIoTWolverineModule>();
        builder.AddModule<GranitIoTNotificationsModule>();
        builder.AddModule<GranitIoTBackgroundJobsModule>();
        builder.AddModule<GranitIoTTimelineModule>();
        builder.AddModule<GranitIoTMcpModule>();
        return builder;
    }
}
