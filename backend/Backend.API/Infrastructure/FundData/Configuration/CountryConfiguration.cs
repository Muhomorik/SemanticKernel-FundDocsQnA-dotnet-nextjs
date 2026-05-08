using Backend.API.Domain.FundData.Models;
using Backend.API.Infrastructure.FundData.Configuration.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.API.Infrastructure.FundData.Configuration;

/// <summary>
/// EF Core configuration for the <see cref="Country"/> lookup table.
/// </summary>
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
