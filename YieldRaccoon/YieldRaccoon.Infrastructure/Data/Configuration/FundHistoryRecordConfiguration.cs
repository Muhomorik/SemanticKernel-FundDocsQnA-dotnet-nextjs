using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

namespace YieldRaccoon.Infrastructure.Data.Configuration;

/// <summary>
/// EF Core configuration for <see cref="FundHistoryRecord"/> entity.
/// </summary>
/// <remarks>
/// <para>
/// Maps historical time-series data to the FundHistoryRecords table.
/// Each record represents a point-in-time snapshot of fund metrics (NAV, capital, risk, etc.).
/// </para>
/// <para>
/// Indexing strategy optimized for time-range queries per fund:
/// - Composite index on (FundId, NavDate DESC) for efficient "get fund X history between dates" queries
/// - Unique constraint prevents duplicate snapshots for the same fund on the same date
/// </para>
/// <para>
/// SQLite type mappings:
/// - Decimal properties use REAL for numeric comparisons and smaller storage
/// - DateOnly properties use TEXT in ISO 8601 format (YYYY-MM-DD)
/// - FundId uses TEXT with fixed 12-character length (ISIN format)
/// </para>
/// </remarks>
public class FundHistoryRecordConfiguration : IEntityTypeConfiguration<FundHistoryRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FundHistoryRecord> builder)
    {
        builder.ToTable("FundHistoryRecords");

        // Auto-increment integer primary key for efficient joins
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .HasConversion<FundHistoryRecordIdConverter>()
            .ValueGeneratedOnAdd();

        // Foreign key to FundProfile (ISIN, 12-character fixed length)
        builder.Property(h => h.IsinId)
            .HasConversion<FundIdConverter>()
            .HasColumnName("FundId")
            .HasMaxLength(12)
            .IsFixedLength()
            .IsRequired();

        // Net Asset Value per share
        builder.Property(h => h.Nav)
            .HasColumnType("REAL");

        // Date when NAV was calculated (one NAV per fund per day)
        builder.Property(h => h.NavDate)
            .HasColumnType("TEXT")
            .HasMaxLength(10);

        // Total assets under management in fund currency
        builder.Property(h => h.Capital)
            .HasColumnType("REAL");

        // Risk-adjusted return metric (excess return / standard deviation)
        builder.Property(h => h.SharpeRatio)
            .HasColumnType("REAL");

        // Volatility measure (annualized percentage deviation from mean)
        builder.Property(h => h.StandardDeviation)
            .HasColumnType("REAL");

        // Composite index for time-range queries: "Get fund X history between dates Y and Z"
        // Ordered DESC on NavDate for efficient "latest records" queries
        builder.HasIndex(h => new { FundId = h.IsinId, h.NavDate })
            .HasDatabaseName("IX_FundHistoryRecords_FundId_NavDate")
            .IsDescending(false, true);

        // Unique constraint: only one record per fund per NAV date
        // Prevents duplicate snapshots from multiple crawler runs
        builder.HasIndex(h => new { FundId = h.IsinId, h.NavDate })
            .HasDatabaseName("UX_FundHistoryRecords_FundId_NavDate")
            .IsUnique();
    }
}
