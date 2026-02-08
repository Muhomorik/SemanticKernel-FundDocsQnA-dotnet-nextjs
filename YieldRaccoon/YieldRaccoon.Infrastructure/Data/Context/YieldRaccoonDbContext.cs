using Microsoft.EntityFrameworkCore;
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
/// The event store (<see cref="Application.CrawlEvents.ICrawlEventStore"/>) remains in-memory
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

    /// <summary>
    /// Gets or sets the fund profiles (aggregate roots).
    /// </summary>
    public DbSet<FundProfile> FundProfiles => Set<FundProfile>();

    /// <summary>
    /// Gets or sets the fund history records.
    /// </summary>
    public DbSet<FundHistoryRecord> FundHistoryRecords => Set<FundHistoryRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new FundProfileConfiguration());
        modelBuilder.ApplyConfiguration(new FundHistoryRecordConfiguration());
    }
}
