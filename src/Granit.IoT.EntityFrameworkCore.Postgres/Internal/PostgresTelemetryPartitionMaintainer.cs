using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Internal;

/// <summary>
/// PostgreSQL-backed maintainer: queries <c>pg_partitioned_table</c> to detect
/// whether the parent table is partitioned, and emits partition + index DDL via
/// <see cref="TelemetryPartitionSqlBuilder"/> so runtime and migration paths
/// share the same SQL.
/// </summary>
internal sealed class PostgresTelemetryPartitionMaintainer(
    IDbContextFactory<IoTDbContext> contextFactory)
    : ITelemetryPartitionMaintainer
{
    private const string IsPartitionedSql = @"
SELECT EXISTS (
    SELECT 1
    FROM pg_partitioned_table pt
    JOIN pg_class c ON c.oid = pt.partrelid
    WHERE c.relname = 'iot_telemetry_points'
)";

    public async Task<bool> IsParentPartitionedAsync(CancellationToken cancellationToken = default)
    {
        await using IoTDbContext db = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Database
            .SqlQueryRaw<bool>(IsPartitionedSql)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CreatePartitionAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        string sql = TelemetryPartitionSqlBuilder.CreatePartitionSql(year, month);
        await using IoTDbContext db = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Database
            .ExecuteSqlRawAsync(sql, cancellationToken)
            .ConfigureAwait(false);
    }
}
