using Granit.Persistence.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore;

/// <summary>
/// Schema-level constants for the isolated <c>IoTDbContext</c>. The
/// <c>iot_</c> prefix isolates IoT tables from the rest of the host schema.
/// </summary>
public static class GranitIoTDbProperties
{
    /// <summary>Table-name prefix (<c>iot_</c>) stamped on every table owned by the module.</summary>
    public static string DbTablePrefix { get; set; } = "iot_";

    private static string? _dbSchema;
    private static bool _dbSchemaExplicitlySet;

    /// <summary>Schema name holding the module's tables. Falls back to the host schema then the framework default when unset.</summary>
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
