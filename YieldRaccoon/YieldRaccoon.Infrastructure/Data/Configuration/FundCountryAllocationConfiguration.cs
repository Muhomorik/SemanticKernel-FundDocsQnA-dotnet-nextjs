using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

namespace YieldRaccoon.Infrastructure.Data.Configuration;

/// <summary>
/// EF Core configuration for <see cref="FundCountryAllocation"/> entity.
/// </summary>
/// <remarks>
/// <para>
/// Latest-only: a unique index on (IsinId, CountryId) ensures one row per fund per country.
/// Decimal stored as REAL (SQLite). FK to <see cref="FundProfile"/> cascades on delete;
/// FK to <see cref="Country"/> restricts on delete (don't drop a referenced lookup row).
/// </para>
/// <para>
/// No navigation back to <see cref="FundProfile"/> is declared on the principal side —
/// queries always go through this allocation table directly.
/// </para>
/// </remarks>
public class FundCountryAllocationConfiguration : IEntityTypeConfiguration<FundCountryAllocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FundCountryAllocation> builder)
    {
        builder.ToTable("FundCountryAllocations");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion<FundCountryAllocationIdConverter>()
            .HasColumnName("FundCountryAllocationId")
            .IsRequired();

        builder.Property(a => a.IsinId)
            .HasConversion<FundIdConverter>()
            .HasColumnName("Isin")
            .HasMaxLength(12)
            .IsFixedLength()
            .IsRequired();

        builder.Property(a => a.CountryId)
            .HasConversion<CountryIdConverter>()
            .HasColumnName("CountryId")
            .IsRequired();

        builder.Property(a => a.Percentage)
            .HasColumnType("REAL")
            .IsRequired();

        builder.HasIndex(a => new { a.IsinId, a.CountryId })
            .HasDatabaseName("UX_FundCountryAllocations_Isin_CountryId")
            .IsUnique();

        builder.HasOne<FundProfile>()
            .WithMany()
            .HasForeignKey(a => a.IsinId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Country>()
            .WithMany()
            .HasForeignKey(a => a.CountryId)
            .HasPrincipalKey(c => c.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
