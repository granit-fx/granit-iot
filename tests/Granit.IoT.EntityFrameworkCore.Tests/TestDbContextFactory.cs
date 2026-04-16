using System.Text.Json;
using Granit.IoT.EntityFrameworkCore.Internal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Granit.IoT.EntityFrameworkCore.Tests;

/// <summary>
/// Creates <see cref="IoTDbContext"/> instances backed by SQLite in-memory
/// for fast, isolated integration tests that support all relational operations
/// (including <c>ExecuteUpdateAsync</c> / <c>ExecuteDeleteAsync</c>).
/// </summary>
internal sealed class TestDbContextFactory : IDbContextFactory<IoTDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IoTDbContext> _options;

    private TestDbContextFactory(SqliteConnection connection, DbContextOptions<IoTDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static TestDbContextFactory Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var optionsBuilder = new DbContextOptionsBuilder<IoTDbContext>();
        optionsBuilder.UseSqlite(connection);
        optionsBuilder.ReplaceService<IModelCustomizer, SqliteCompatibleModelCustomizer>();

        DbContextOptions<IoTDbContext> options = optionsBuilder.Options;

        using (var db = new IoTDbContext(options))
        {
            db.Database.EnsureCreated();
        }

        return new TestDbContextFactory(connection, options);
    }

    public IoTDbContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}

/// <summary>
/// Model customizer that remaps PostgreSQL-specific types (jsonb, DateTimeOffset)
/// to SQLite-compatible types with value converters.
/// </summary>
internal sealed class SqliteCompatibleModelCustomizer(ModelCustomizerDependencies dependencies)
    : RelationalModelCustomizer(dependencies)
{
    private static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetConverter = new(
        v => v.ToUnixTimeMilliseconds(),
        v => DateTimeOffset.FromUnixTimeMilliseconds(v));

    private static readonly ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetConverter = new(
        v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : null,
        v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

    private static readonly ValueConverter<IReadOnlyDictionary<string, double>, string> MetricsConverter = new(
        v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
        v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, JsonSerializerOptions.Default)!);

    private static readonly ValueConverter<Dictionary<string, string>?, string?> TagsConverter = new(
        v => v != null ? JsonSerializer.Serialize(v, JsonSerializerOptions.Default) : null,
        v => v != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonSerializerOptions.Default) : null);

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(DateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(NullableDateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(IReadOnlyDictionary<string, double>))
                {
                    property.SetColumnType("TEXT");
                    property.SetValueConverter(MetricsConverter);
                }
                else if (property.ClrType == typeof(Dictionary<string, string>))
                {
                    property.SetColumnType("TEXT");
                    property.SetValueConverter(TagsConverter);
                }
            }
        }
    }
}
