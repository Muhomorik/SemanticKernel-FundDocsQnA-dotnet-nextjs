using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Configuration;

namespace YieldRaccoon.Infrastructure.Data.Context;

/// <summary>
/// Entity Framework Core database context for YieldRaccoon.
/// </summary>
/// <remarks>
/// <para>
/// This context manages persistent storage of fund data using SQLite.
/// It contains two main entity sets:
/// <list type="bullet">
///     <item><see cref="FundProfiles"/> - Static fund information (aggregate root)</item>
///     <item><see cref="FundHistoryRecords"/> - Time-series historical data</item>
/// </list>
/// </para>
/// <para>
/// The event store (<see cref="Application.Services.IFundListEventStore"/>) remains in-memory
/// and is not managed by this context.
/// </para>
/// </remarks>
public class YieldRaccoonDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YieldRaccoonDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public YieldRaccoonDbContext(DbContextOptions<YieldRaccoonDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Suppress the PendingModelChangesWarning. EF Core 9 runtime + EF tools 10 disagree
        // on whether SQLite INTEGER PRIMARY KEY auto-increments require an explicit
        // Sqlite:Autoincrement annotation, producing a phantom "model has changed" diff
        // for FundHistoryRecord.Id that never affects schema. We deliberately do not chase
        // this drift with new migrations — see comments on the AddFundAllocations migration.
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    /// <summary>
    /// Gets or sets the fund profiles (aggregate roots).
    /// </summary>
    public DbSet<FundProfile> FundProfiles => Set<FundProfile>();

    /// <summary>
    /// Gets or sets the fund history records.
    /// </summary>
    public DbSet<FundHistoryRecord> FundHistoryRecords => Set<FundHistoryRecord>();

    /// <summary>Country lookup table for portfolio allocations.</summary>
    public DbSet<Country> Countries => Set<Country>();

    /// <summary>Sector lookup table for portfolio allocations.</summary>
    public DbSet<Sector> Sectors => Set<Sector>();

    /// <summary>Per-fund country allocation rows (latest snapshot, no history).</summary>
    public DbSet<FundCountryAllocation> FundCountryAllocations => Set<FundCountryAllocation>();

    /// <summary>Per-fund sector allocation rows (latest snapshot, no history).</summary>
    public DbSet<FundSectorAllocation> FundSectorAllocations => Set<FundSectorAllocation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new FundProfileConfiguration());
        modelBuilder.ApplyConfiguration(new FundHistoryRecordConfiguration());
        modelBuilder.ApplyConfiguration(new CountryConfiguration());
        modelBuilder.ApplyConfiguration(new SectorConfiguration());
        modelBuilder.ApplyConfiguration(new FundCountryAllocationConfiguration());
        modelBuilder.ApplyConfiguration(new FundSectorAllocationConfiguration());
    }
}
