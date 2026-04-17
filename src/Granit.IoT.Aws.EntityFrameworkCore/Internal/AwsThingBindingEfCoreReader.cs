using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.Aws.EntityFrameworkCore.Internal;

internal sealed class AwsThingBindingEfCoreReader(
    IDbContextFactory<AwsBindingDbContext> contextFactory,
    ICurrentTenant? currentTenant = null)
    : EfStoreBase<AwsThingBinding, AwsBindingDbContext>(contextFactory, currentTenant), IAwsThingBindingReader
{
    public Task<AwsThingBinding?> FindByDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default) =>
        FirstOrDefaultAsync(b => b.DeviceId == deviceId, cancellationToken);

    public Task<AwsThingBinding?> FindByThingNameAsync(ThingName thingName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(thingName);
        return FirstOrDefaultAsync(b => b.ThingName == thingName, cancellationToken);
    }

    public Task<IReadOnlyList<AwsThingBinding>> ListByStatusAsync(
        IReadOnlyCollection<AwsThingProvisioningStatus> statuses,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        if (statuses.Count == 0)
        {
            throw new ArgumentException("At least one status must be provided.", nameof(statuses));
        }

        return ReadAsync<IReadOnlyList<AwsThingBinding>>(async db =>
            await Query(db)
                .Where(b => statuses.Contains(b.ProvisioningStatus))
                .OrderBy(b => b.Id)
                .Take(batchSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false),
            cancellationToken);
    }
}
