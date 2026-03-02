using Backend.API.Domain.FundData.Models;
using Backend.API.Infrastructure.FundData.Configuration.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.API.Infrastructure.FundData.Configuration;

/// <summary>
/// EF Core configuration for <see cref="FundHistoryRecord"/> entity.
/// </summary>
/// <remarks>
/// SQL Server type mappings:
/// - Id: BIGINT IDENTITY for auto-increment
/// - FundId (ISIN): NCHAR(12) fixed-length
/// - Decimals: DECIMAL(18,6)
/// - NavDate: DATE
///
/// Indexing strategy for time-range queries per fund:
/// - Composite index on (FundId, NavDate DESC) for efficient history queries
/// - Unique constraint prevents duplicate snapshots for the same fund on the same date
/// </remarks>
public class FundHistoryRecordConfiguration : IEntityTypeConfiguration<FundHistoryRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FundHistoryRecord> builder)
    {
        builder.ToTable("FundHistoryRecords");

        // Auto-increment primary key
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .HasConversion<FundHistoryRecordIdConverter>()
            .ValueGeneratedOnAdd();

        // Foreign key to FundProfile (ISIN, 12-character fixed length)
        builder.Property(h => h.IsinId)
            .HasConversion<IsinIdConverter>()
            .HasColumnName("FundId")
            .HasColumnType("NCHAR(12)")
            .IsFixedLength()
            .IsRequired();

        builder.Property(h => h.Nav).HasColumnType("DECIMAL(18,6)");
        builder.Property(h => h.NavDate).HasColumnType("DATE");
        builder.Property(h => h.Capital).HasColumnType("DECIMAL(18,2)");
        builder.Property(h => h.SharpeRatio).HasColumnType("DECIMAL(18,6)");
        builder.Property(h => h.StandardDeviation).HasColumnType("DECIMAL(18,6)");

        // Composite index for time-range queries (NavDate DESC for "latest records" queries)
        builder.HasIndex(h => new { FundId = h.IsinId, h.NavDate })
            .HasDatabaseName("IX_FundHistoryRecords_FundId_NavDate")
            .IsDescending(false, true);

        // Unique constraint: one record per fund per NAV date
        builder.HasIndex(h => new { FundId = h.IsinId, h.NavDate })
            .HasDatabaseName("UX_FundHistoryRecords_FundId_NavDate")
            .IsUnique();
    }
}
