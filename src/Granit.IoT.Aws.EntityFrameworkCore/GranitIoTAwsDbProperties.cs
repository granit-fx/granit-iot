using Granit.Persistence.EntityFrameworkCore;

namespace Granit.IoT.Aws.EntityFrameworkCore;

/// <summary>
/// Schema-level constants for the AWS bridge's isolated EF Core context.
/// The <c>iotaws_</c> table prefix keeps the bridge's tables visually
/// separated from the core IoT schema and lets a deployment that no longer
/// uses AWS drop the bridge's tables without touching the core schema.
/// </summary>
public static class GranitIoTAwsDbProperties
{
    /// <summary>Table-name prefix (<c>iotaws_</c>) stamped on every table owned by the AWS bridge.</summary>
    public static string DbTablePrefix { get; set; } = "iotaws_";

    private static string? _dbSchema;
    private static bool _dbSchemaExplicitlySet;

    /// <summary>Schema name holding the bridge's tables. Falls back to the host schema then the framework default when unset.</summary>
    public static string? DbSchema
    {
        get => _dbSchemaExplicitlySet
            ? _dbSchema
            : GranitDbDefaults.HostDbSchema ?? GranitDbDefaults.DbSchema;
        set
        {
            _dbSchema = value;
            _dbSchemaExplicitlySet = true;
        }
    }
}
