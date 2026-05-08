using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

namespace YieldRaccoon.Infrastructure.Data.Configuration;

/// <summary>
/// EF Core configuration for <see cref="Sector"/> entity.
/// </summary>
/// <remarks>
/// Maps the lookup row to the Sectors table. <c>DisplayName</c> is unique-indexed so
/// the get-or-create pattern can use it as the natural key.
/// </remarks>
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
