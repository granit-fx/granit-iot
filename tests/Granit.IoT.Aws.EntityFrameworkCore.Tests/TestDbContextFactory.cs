using Granit.IoT.Aws.EntityFrameworkCore.Internal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Granit.IoT.Aws.EntityFrameworkCore.Tests;

/// <summary>
/// Backs <see cref="AwsBindingDbContext"/> with a SQLite in-memory connection
/// for fast, isolated integration tests. SQLite supports the relational
/// surface used by the EF Core reader/writer (<c>ExecuteUpdateAsync</c>,
/// <c>ExecuteDeleteAsync</c>) so we don't need Testcontainers here.
/// </summary>
internal sealed class TestDbContextFactory : IDbContextFactory<AwsBindingDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AwsBindingDbContext> _options;

    private TestDbContextFactory(SqliteConnection connection, DbContextOptions<AwsBindingDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static TestDbContextFactory Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var optionsBuilder = new DbContextOptionsBuilder<AwsBindingDbContext>();
        optionsBuilder.UseSqlite(connection);

        DbContextOptions<AwsBindingDbContext> options = optionsBuilder.Options;

        using (var db = new AwsBindingDbContext(options))
        {
            db.Database.EnsureCreated();
        }

        return new TestDbContextFactory(connection, options);
    }

    public AwsBindingDbContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
