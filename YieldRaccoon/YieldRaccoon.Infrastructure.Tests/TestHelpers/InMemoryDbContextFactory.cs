using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Factory for creating <see cref="YieldRaccoonDbContext"/> instances backed by an
/// in-memory SQLite database — the same provider production uses.
/// </summary>
/// <remarks>
/// <para>
/// Tests historically used the EF Core <c>InMemory</c> provider, but that provider is
/// permissive: it silently accepts LINQ queries that the real SQLite provider cannot
/// translate (notably <see cref="DateTimeOffset"/> comparisons and ORDER BY expressions).
/// Running tests against real SQLite catches translation failures during CI instead of
/// at runtime in production.
/// </para>
/// <para>
/// Each <see cref="Create"/> call opens a fresh <c>:memory:</c> database whose lifetime
/// is tied to a single <see cref="SqliteConnection"/>. The connection is owned by the
/// returned context wrapper and disposed when the context is disposed.
/// </para>
/// </remarks>
public static class InMemoryDbContextFactory
{
    /// <summary>
    /// Creates a new <see cref="YieldRaccoonDbContext"/> backed by a fresh SQLite
    /// in-memory database. Dispose the returned context to release the database.
    /// </summary>
    public static YieldRaccoonDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<YieldRaccoonDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestYieldRaccoonDbContext(options, connection);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class TestYieldRaccoonDbContext : YieldRaccoonDbContext
    {
        private readonly SqliteConnection _connection;

        public TestYieldRaccoonDbContext(
            DbContextOptions<YieldRaccoonDbContext> options,
            SqliteConnection connection)
            : base(options)
        {
            _connection = connection;
        }

        public override void Dispose()
        {
            base.Dispose();
            _connection.Dispose();
        }
    }
}
