using Granit.IoT.Abstractions;
using Granit.IoT.EntityFrameworkCore;
using Granit.IoT.EntityFrameworkCore.Internal;
using Granit.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Granit.IoT.EntityFrameworkCore.Postgres.Internal;

/// <summary>
/// PostgreSQL-specific telemetry reader. Overrides
/// <see cref="TelemetryEfCoreReader.GetAggregateAsync"/> to push metric-level
/// aggregation into the database using JSONB extraction
/// (<c>("Metrics" -&gt;&gt; 'name')::double precision</c>) — portable LINQ
/// cannot translate indexer/<c>ContainsKey</c> on a JSONB-mapped
/// <see cref="IReadOnlyDictionary{TKey,TValue}"/>.
/// </summary>
internal sealed class PostgresTelemetryEfCoreReader(
    IDbContextFactory<IoTDbContext> contextFactory,
    ICurrentTenant? currentTenant = null)
    : TelemetryEfCoreReader(contextFactory, currentTenant)
{
    private readonly ICurrentTenant? _currentTenant = currentTenant;

    public override async Task<TelemetryAggregate?> GetAggregateAsync(
        Guid deviceId,
        string metricName,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        TelemetryAggregation aggregation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(metricName);

        return await ReadAsync(async db =>
        {
            string table = QualifiedTelemetryTable();
            string tenantPredicate = BuildTenantPredicate(out Guid? tenantFilterValue);

            string aggregateExpr = aggregation switch
            {
                TelemetryAggregation.Avg => "AVG((\"Metrics\" ->> @metric)::double precision)",
                TelemetryAggregation.Min => "MIN((\"Metrics\" ->> @metric)::double precision)",
                TelemetryAggregation.Max => "MAX((\"Metrics\" ->> @metric)::double precision)",
                TelemetryAggregation.Count => "COUNT(*)::double precision",
                _ => throw new ArgumentOutOfRangeException(nameof(aggregation), aggregation, null),
            };

            string sql =
                $"SELECT {aggregateExpr} AS \"Value\", COUNT(*) AS \"Count\" " +
                $"FROM {table} " +
                "WHERE \"DeviceId\" = @deviceId " +
                "  AND \"RecordedAt\" >= @rangeStart " +
                "  AND \"RecordedAt\" <= @rangeEnd " +
                "  AND \"Metrics\" ? @metric" +
                tenantPredicate;

            List<NpgsqlParameter> parameters =
            [
                new("deviceId", deviceId),
                new("metric", metricName),
                new("rangeStart", rangeStart),
                new("rangeEnd", rangeEnd),
            ];
            if (tenantFilterValue is { } tenantValue)
            {
                parameters.Add(new NpgsqlParameter("tenantId", tenantValue));
            }

            AggregateRow? row = await db.Database
                .SqlQueryRaw<AggregateRow>(sql, [.. parameters])
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (row is null || row.Count == 0)
            {
                return null;
            }

            return new TelemetryAggregate(row.Value ?? 0d, row.Count, rangeStart, rangeEnd);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static string QualifiedTelemetryTable()
    {
        string tableName = GranitIoTDbProperties.DbTablePrefix + "telemetry_points";
        string? schema = GranitIoTDbProperties.DbSchema;
        return string.IsNullOrEmpty(schema)
            ? $"\"{tableName}\""
            : $"\"{schema}\".\"{tableName}\"";
    }

    private string BuildTenantPredicate(out Guid? tenantFilterValue)
    {
        // Raw SQL bypasses EF Core query filters. Replicate the multi-tenant filter
        // behavior: when a tenant is active, constrain to its rows. When running in
        // host context (currentTenant is provided but not available), no constraint
        // is applied — matches the IgnoreQueryFilters bypass in EfStoreBase.Query.
        if (_currentTenant is { IsAvailable: true, Id: { } tenantId })
        {
            tenantFilterValue = tenantId;
            return " AND \"TenantId\" = @tenantId";
        }

        tenantFilterValue = null;
        return string.Empty;
    }

    private sealed record AggregateRow(double? Value, long Count);
}
