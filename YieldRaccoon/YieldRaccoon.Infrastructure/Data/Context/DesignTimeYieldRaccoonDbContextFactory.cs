using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YieldRaccoon.Infrastructure.Data.Context;

/// <summary>
/// Design-time factory for the EF Core CLI tools (e.g., <c>dotnet ef migrations add</c>).
/// </summary>
/// <remarks>
/// The runtime app builds the <see cref="YieldRaccoonDbContext"/> through Autofac, which the EF
/// tooling cannot discover. This factory provides a minimal SQLite-configured context so the
/// migrations generator can introspect the model. The connection string is never opened at
/// design time — it only needs to be valid SQLite syntax for provider selection.
/// </remarks>
public class DesignTimeYieldRaccoonDbContextFactory : IDesignTimeDbContextFactory<YieldRaccoonDbContext>
{
    /// <inheritdoc />
    public YieldRaccoonDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<YieldRaccoonDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new YieldRaccoonDbContext(options);
    }
}
