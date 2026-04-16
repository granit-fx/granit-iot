using Granit.Persistence.EntityFrameworkCore;

namespace Granit.IoT.EntityFrameworkCore;

public static class GranitIoTDbProperties
{
    public static string DbTablePrefix { get; set; } = "iot_";

    private static string? _dbSchema;
    private static bool _dbSchemaExplicitlySet;

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
