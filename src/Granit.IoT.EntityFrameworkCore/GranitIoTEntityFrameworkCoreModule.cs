using Granit.Modularity;
using Granit.Persistence;

namespace Granit.IoT.EntityFrameworkCore;

/// <summary>
/// EF Core persistence module for <c>IoTDbContext</c> (Device + TelemetryPoint entities,
/// isolated schema). Provider registration (PostgreSQL, TimescaleDB) is done by the
/// companion provider modules.
/// </summary>
[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitPersistenceModule))]
public sealed class GranitIoTEntityFrameworkCoreModule : GranitModule;
