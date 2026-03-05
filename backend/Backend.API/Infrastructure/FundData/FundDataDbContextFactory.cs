using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backend.API.Infrastructure.FundData;

/// <summary>
/// Design-time factory for <see cref="FundDataDbContext"/>.
/// Used by <c>dotnet ef migrations</c> when the DbContext isn't registered in DI at design time.
/// </summary>
public class FundDataDbContextFactory : IDesignTimeDbContextFactory<FundDataDbContext>
{
    public FundDataDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FundDataDbContext>();

        // Design-time only: uses a dummy connection string for migration scaffolding.
        // The actual connection string comes from BackendOptions at runtime.
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=FundData_DesignTime;Trusted_Connection=True;");

        return new FundDataDbContext(optionsBuilder.Options);
    }
}
