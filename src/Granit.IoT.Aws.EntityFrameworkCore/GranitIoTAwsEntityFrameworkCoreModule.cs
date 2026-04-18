using Granit.Modularity;
using Granit.Persistence;

namespace Granit.IoT.Aws.EntityFrameworkCore;

/// <summary>
/// EF Core persistence for the AWS IoT bridge: <c>AwsBindingDbContext</c> holding the
/// <c>AwsThingBinding</c> companion entity (iotaws_* schema, isolated from the main
/// IoT schema so the bridge can be added or removed without migrations on core tables).
/// </summary>
[DependsOn(typeof(GranitIoTAwsModule))]
[DependsOn(typeof(GranitPersistenceModule))]
public sealed class GranitIoTAwsEntityFrameworkCoreModule : GranitModule;
