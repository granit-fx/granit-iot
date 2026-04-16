using Granit.Modularity;
using Granit.Persistence;

namespace Granit.IoT.EntityFrameworkCore;

[DependsOn(typeof(GranitIoTModule))]
[DependsOn(typeof(GranitPersistenceModule))]
public sealed class GranitIoTEntityFrameworkCoreModule : GranitModule;
