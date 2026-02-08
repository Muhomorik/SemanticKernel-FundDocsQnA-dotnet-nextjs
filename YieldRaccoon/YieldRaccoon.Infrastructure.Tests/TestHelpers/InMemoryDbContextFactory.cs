using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Factory for creating EF Core InMemory database contexts for testing.
/// </summary>
public static class InMemoryDbContextFactory
{
    /// <summary>
    /// Creates a new <see cref="YieldRaccoonDbContext"/> using the InMemory provider.
    /// </summary>
    /// <param name="databaseName">Optional database name. If not provided, a unique name is generated.</param>
    /// <returns>A new database context instance.</returns>
    public static YieldRaccoonDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<YieldRaccoonDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new YieldRaccoonDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
