using Backend.API.Domain.FundData.Models;
using Backend.API.Infrastructure.FundData.Configuration.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.API.Infrastructure.FundData.Configuration;

/// <summary>
/// EF Core configuration for <see cref="FundCountryAllocation"/>.
/// </summary>
/// <remarks>
/// SQL Server type mappings: GUID PKs as UNIQUEIDENTIFIER, Percentage as DECIMAL(5,2).
/// Cascade-deleted with FundProfile; restricted-on-delete from Country.
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
            .HasConversion<IsinIdConverter>()
            .HasColumnName("Isin")
            .HasColumnType("NCHAR(12)")
            .IsFixedLength()
            .IsRequired();

        builder.Property(a => a.CountryId)
            .HasConversion<CountryIdConverter>()
            .HasColumnName("CountryId")
            .IsRequired();

        builder.Property(a => a.Percentage)
            .HasColumnType("DECIMAL(5,2)")
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
