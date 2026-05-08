using Backend.API.Domain.FundData.Models;
using Backend.API.Infrastructure.FundData.Configuration.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.API.Infrastructure.FundData.Configuration;

/// <summary>
/// EF Core configuration for the <see cref="Sector"/> lookup table.
/// </summary>
public class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        builder.ToTable("Sectors");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion<SectorIdConverter>()
            .HasColumnName("SectorId")
            .IsRequired();

        builder.Property(s => s.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(s => s.DisplayName)
            .HasDatabaseName("UX_Sectors_DisplayName")
            .IsUnique();
    }
}
