using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

namespace YieldRaccoon.Infrastructure.Data.Configuration;

/// <summary>
/// EF Core configuration for <see cref="FundSectorAllocation"/> entity.
/// </summary>
/// <remarks>
/// <para>
/// Latest-only: a unique index on (IsinId, SectorId) ensures one row per fund per sector.
/// Decimal stored as REAL (SQLite). FK to <see cref="FundProfile"/> cascades on delete;
/// FK to <see cref="Sector"/> restricts on delete (don't drop a referenced lookup row).
/// </para>
/// </remarks>
public class FundSectorAllocationConfiguration : IEntityTypeConfiguration<FundSectorAllocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FundSectorAllocation> builder)
    {
        builder.ToTable("FundSectorAllocations");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion<FundSectorAllocationIdConverter>()
            .HasColumnName("FundSectorAllocationId")
            .IsRequired();

        builder.Property(a => a.IsinId)
            .HasConversion<FundIdConverter>()
            .HasColumnName("Isin")
            .HasMaxLength(12)
            .IsFixedLength()
            .IsRequired();

        builder.Property(a => a.SectorId)
            .HasConversion<SectorIdConverter>()
            .HasColumnName("SectorId")
            .IsRequired();

        builder.Property(a => a.Percentage)
            .HasColumnType("REAL")
            .IsRequired();

        builder.HasIndex(a => new { a.IsinId, a.SectorId })
            .HasDatabaseName("UX_FundSectorAllocations_Isin_SectorId")
            .IsUnique();

        builder.HasOne<FundProfile>()
            .WithMany()
            .HasForeignKey(a => a.IsinId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Sector>()
            .WithMany()
            .HasForeignKey(a => a.SectorId)
            .HasPrincipalKey(s => s.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
