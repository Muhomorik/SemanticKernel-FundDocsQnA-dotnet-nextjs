using Backend.API.Domain.FundData.Models;
using Backend.API.Infrastructure.FundData.Configuration.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.API.Infrastructure.FundData.Configuration;

/// <summary>
/// EF Core configuration for <see cref="FundProfile"/> entity.
/// </summary>
/// <remarks>
/// Maps to FundProfiles table with ISIN as primary key.
/// SQL Server type mappings (instead of SQLite REAL/TEXT):
/// - ISIN: NCHAR(12) fixed-length
/// - Decimals: DECIMAL(18,6) for precision
/// - Dates: DATE for date-only values
/// </remarks>
public class FundProfileConfiguration : IEntityTypeConfiguration<FundProfile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FundProfile> builder)
    {
        builder.ToTable("FundProfiles");

        // Primary key: ISIN (12-character international securities identifier)
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasConversion<IsinIdConverter>()
            .HasColumnName("Isin")
            .HasColumnType("NCHAR(12)")
            .IsFixedLength()
            .IsRequired();

        builder.Property(f => f.Name)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(f => f.OrderbookId)
            .HasMaxLength(50);

        builder.Property(f => f.Category)
            .HasMaxLength(200);

        builder.Property(f => f.CompanyName)
            .HasMaxLength(200);

        builder.Property(f => f.FundType)
            .HasMaxLength(50);

        builder.Property(f => f.CurrencyCode)
            .HasMaxLength(10);

        builder.Property(f => f.ManagedType)
            .HasMaxLength(20);

        builder.Property(f => f.StartDate)
            .HasColumnType("DATE");

        builder.Property(f => f.RecommendedHoldingPeriod)
            .HasMaxLength(50);

        // Fees and financial metrics — DECIMAL(18,6) for precision
        builder.Property(f => f.ManagementFee).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.TotalFee).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.TransactionFee).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.OngoingFee).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.MinimumBuy).HasColumnType("DECIMAL(18,2)");
        builder.Property(f => f.Capital).HasColumnType("DECIMAL(18,2)");
        builder.Property(f => f.SharpeRatio).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.StandardDeviation).HasColumnType("DECIMAL(18,6)");

        // Sustainability scores
        builder.Property(f => f.SustainabilityLevel).HasMaxLength(20);
        builder.Property(f => f.EsgScore).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.EnvironmentalScore).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.SocialScore).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.GovernanceScore).HasColumnType("DECIMAL(18,6)");
        builder.Property(f => f.EuArticleType).HasMaxLength(50);

        // Fund description text from fund-reference API
        builder.Property(f => f.Description).HasMaxLength(4000);

        // One-to-many relationship with historical snapshots
        builder.HasMany(f => f.HistoryRecords)
            .WithOne(h => h.FundProfile)
            .HasForeignKey(h => h.IsinId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
