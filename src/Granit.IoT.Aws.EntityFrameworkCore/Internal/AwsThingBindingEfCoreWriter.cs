using Granit.IoT.Aws.Abstractions;
using Granit.IoT.Aws.Domain;
using Granit.MultiTenancy;
using Granit.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.Aws.EntityFrameworkCore.Internal;

internal sealed class AwsThingBindingEfCoreWriter(
    IDbContextFactory<AwsBindingDbContext> contextFactory,
    ICurrentTenant? currentTenant = null)
    : EfStoreBase<AwsThingBinding, AwsBindingDbContext>(contextFactory, currentTenant), IAwsThingBindingWriter
{
    public new Task AddAsync(AwsThingBinding binding, CancellationToken cancellationToken = default) =>
        base.AddAsync(binding, cancellationToken);

    public new Task UpdateAsync(AwsThingBinding binding, CancellationToken cancellationToken = default) =>
        base.UpdateAsync(binding, cancellationToken);

    public new Task DeleteAsync(AwsThingBinding binding, CancellationToken cancellationToken = default) =>
        base.DeleteAsync(binding, cancellationToken);
}
