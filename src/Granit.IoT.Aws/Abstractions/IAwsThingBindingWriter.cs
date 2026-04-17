using Granit.IoT.Aws.Domain;

namespace Granit.IoT.Aws.Abstractions;

/// <summary>Persists <see cref="AwsThingBinding"/> rows (command side of CQRS).</summary>
public interface IAwsThingBindingWriter
{
    /// <summary>Persists a newly reserved binding (typically in <c>Pending</c> status).</summary>
    Task AddAsync(AwsThingBinding binding, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing binding (saga progression, shadow timestamp, ...).</summary>
    Task UpdateAsync(AwsThingBinding binding, CancellationToken cancellationToken = default);

    /// <summary>Removes a binding once the matching AWS resources have been deleted.</summary>
    Task DeleteAsync(AwsThingBinding binding, CancellationToken cancellationToken = default);
}
