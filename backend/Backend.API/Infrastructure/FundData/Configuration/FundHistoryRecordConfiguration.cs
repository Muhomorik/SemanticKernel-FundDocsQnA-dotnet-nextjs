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
/// Indexing strategy:
/// - Single unique index on (FundId ASC, NavDate DESC) serves both uniqueness and time-range query performance
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

        // Unique constraint + descending NavDate for efficient "latest records" range queries.
        // A single unique descending index serves both uniqueness enforcement and query performance.
        builder.HasIndex(h => new { FundId = h.IsinId, h.NavDate })
            .HasDatabaseName("UX_FundHistoryRecords_FundId_NavDate")
            .IsUnique()
            .IsDescending(false, true);
    }
}
