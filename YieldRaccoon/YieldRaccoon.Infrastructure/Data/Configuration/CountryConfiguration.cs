using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

namespace YieldRaccoon.Infrastructure.Data.Configuration;

/// <summary>
/// EF Core configuration for <see cref="Country"/> entity.
/// </summary>
/// <remarks>
/// Maps the lookup row to the Countries table. <c>DisplayName</c> is unique-indexed so
/// the get-or-create pattern can use it as the natural key.
/// </remarks>
public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion<CountryIdConverter>()
            .HasColumnName("CountryId")
            .IsRequired();

        builder.Property(c => c.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.CountryCode)
            .HasMaxLength(2);

        builder.HasIndex(c => c.DisplayName)
            .HasDatabaseName("UX_Countries_DisplayName")
            .IsUnique();
    }
}
