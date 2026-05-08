using Backend.API.Domain.FundData.Models;
using Backend.API.Infrastructure.FundData.Configuration.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.API.Infrastructure.FundData.Configuration;

/// <summary>
/// EF Core configuration for <see cref="FundSectorAllocation"/>.
/// </summary>
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
            .HasConversion<IsinIdConverter>()
            .HasColumnName("Isin")
            .HasColumnType("NCHAR(12)")
            .IsFixedLength()
            .IsRequired();

        builder.Property(a => a.SectorId)
            .HasConversion<SectorIdConverter>()
            .HasColumnName("SectorId")
            .IsRequired();

        builder.Property(a => a.Percentage)
            .HasColumnType("DECIMAL(5,2)")
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
