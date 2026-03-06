using Backend.API.Infrastructure.FundData;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.TestInfrastructure;

/// <summary>
/// Implements <see cref="IDbContextFactory{TContext}"/> backed by EF Core's InMemory database.
/// Used by <see cref="Backend.API.Infrastructure.FundData.Plugins.FundDataPlugin"/> tests
/// to provide isolated, seedable database contexts.
/// </summary>
public class TestFundDataDbContextFactory : IDbContextFactory<FundDataDbContext>
{
    private readonly DbContextOptions<FundDataDbContext> _options;

    public TestFundDataDbContextFactory(string? databaseName = null)
    {
        _options = new DbContextOptionsBuilder<FundDataDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
    }

    public FundDataDbContext CreateDbContext() => new(_options);
}
