using Granit.Modularity;
using Granit.Persistence;

namespace Granit.IoT.Aws.EntityFrameworkCore;

[DependsOn(typeof(GranitIoTAwsModule))]
[DependsOn(typeof(GranitPersistenceModule))]
public sealed class GranitIoTAwsEntityFrameworkCoreModule : GranitModule;
