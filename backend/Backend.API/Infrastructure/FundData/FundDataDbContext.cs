using Backend.API.Domain.FundData.Models;
using Backend.API.Infrastructure.FundData.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure.FundData;

/// <summary>
/// EF Core database context for fund data persistence in Azure SQL.
/// </summary>
public class FundDataDbContext : DbContext
{
    public FundDataDbContext(DbContextOptions<FundDataDbContext> options) : base(options)
    {
    }

    public DbSet<FundProfile> FundProfiles => Set<FundProfile>();
    public DbSet<FundHistoryRecord> FundHistoryRecords => Set<FundHistoryRecord>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<FundCountryAllocation> FundCountryAllocations => Set<FundCountryAllocation>();
    public DbSet<FundSectorAllocation> FundSectorAllocations => Set<FundSectorAllocation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FundProfileConfiguration());
        modelBuilder.ApplyConfiguration(new FundHistoryRecordConfiguration());
        modelBuilder.ApplyConfiguration(new CountryConfiguration());
        modelBuilder.ApplyConfiguration(new SectorConfiguration());
        modelBuilder.ApplyConfiguration(new FundCountryAllocationConfiguration());
        modelBuilder.ApplyConfiguration(new FundSectorAllocationConfiguration());
    }

}
