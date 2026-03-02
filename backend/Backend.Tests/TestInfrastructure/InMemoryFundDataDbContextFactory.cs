using Backend.API.Infrastructure.FundData;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.TestInfrastructure;

/// <summary>
/// Creates <see cref="FundDataDbContext"/> instances backed by EF Core's InMemory database.
/// Each test gets a unique database name to ensure isolation.
/// </summary>
public static class InMemoryFundDataDbContextFactory
{
    public static FundDataDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<FundDataDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new FundDataDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
