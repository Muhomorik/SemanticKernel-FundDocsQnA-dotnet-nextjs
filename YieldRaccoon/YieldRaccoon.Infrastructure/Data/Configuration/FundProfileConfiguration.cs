using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

namespace YieldRaccoon.Infrastructure.Data.Configuration;

/// <summary>
/// EF Core configuration for <see cref="FundProfile"/> entity.
/// </summary>
/// <remarks>
/// <para>
/// Maps the aggregate root to the FundProfiles table with ISIN as primary key.
/// Contains static fund data (name, category, fees, sustainability scores) that rarely changes.
/// </para>
/// <para>
/// SQLite type mappings:
/// - Decimal properties use REAL for numeric comparisons and smaller storage
/// - DateOnly properties use TEXT in ISO 8601 format (YYYY-MM-DD)
/// - FundId (ISIN) uses TEXT with fixed 12-character length
/// </para>
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
            .HasConversion<FundIdConverter>()
            .HasColumnName("Isin")
            .HasMaxLength(12)
            .IsFixedLength()
            .IsRequired();

        // Fund display name
        builder.Property(f => f.Name)
            .HasMaxLength(500)
            .IsRequired();

        // External orderbook identifier from data source
        builder.Property(f => f.OrderbookId)
            .HasMaxLength(50);

        // Fund category classification
        builder.Property(f => f.Category)
            .HasMaxLength(200);

        // Fund management company name
        builder.Property(f => f.CompanyName)
            .HasMaxLength(200);

        // Fund type (e.g., equity, bond, mixed)
        builder.Property(f => f.FundType)
            .HasMaxLength(50);

        // ISO 4217 currency code (e.g., SEK, EUR, USD)
        builder.Property(f => f.CurrencyCode)
            .HasMaxLength(10);

        // Management type: ACTIVE or PASSIVE
        builder.Property(f => f.ManagedType)
            .HasMaxLength(20);

        // Fund inception date
        builder.Property(f => f.StartDate)
            .HasColumnType("TEXT")
            .HasMaxLength(10);

        // Recommended holding period for investors
        builder.Property(f => f.RecommendedHoldingPeriod)
            .HasMaxLength(50);

        // Annual management fee as decimal (e.g., 0.0125 = 1.25%)
        builder.Property(f => f.ManagementFee)
            .HasColumnType("REAL");

        // Total expense ratio as decimal
        builder.Property(f => f.TotalFee)
            .HasColumnType("REAL");

        // Transaction fee as decimal
        builder.Property(f => f.TransactionFee)
            .HasColumnType("REAL");

        // Ongoing charges as decimal
        builder.Property(f => f.OngoingFee)
            .HasColumnType("REAL");

        // Minimum purchase amount in fund currency
        builder.Property(f => f.MinimumBuy)
            .HasColumnType("REAL");

        // Total assets under management
        builder.Property(f => f.Capital)
            .HasColumnType("REAL");

        // Risk-adjusted return metric
        builder.Property(f => f.SharpeRatio)
            .HasColumnType("REAL");

        // Annualized volatility measure
        builder.Property(f => f.StandardDeviation)
            .HasColumnType("REAL");

        // Sustainability level: WORSE, AVERAGE, BETTER
        builder.Property(f => f.SustainabilityLevel)
            .HasMaxLength(20);

        // Overall ESG score
        builder.Property(f => f.EsgScore)
            .HasColumnType("REAL");

        // Environmental pillar score
        builder.Property(f => f.EnvironmentalScore)
            .HasColumnType("REAL");

        // Social pillar score
        builder.Property(f => f.SocialScore)
            .HasColumnType("REAL");

        // Governance pillar score
        builder.Property(f => f.GovernanceScore)
            .HasColumnType("REAL");

        // EU SFDR classification (Article 6, 8, or 9)
        builder.Property(f => f.EuArticleType)
            .HasMaxLength(50);

        // One-to-many relationship with historical snapshots
        builder.HasMany(f => f.HistoryRecords)
            .WithOne(h => h.FundProfile)
            .HasForeignKey(h => h.FundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
