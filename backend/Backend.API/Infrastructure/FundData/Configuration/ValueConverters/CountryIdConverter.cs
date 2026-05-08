using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.API.Infrastructure.FundData.Configuration.ValueConverters;

/// <summary>EF Core value converter for <see cref="CountryId"/> ↔ <see cref="Guid"/>.</summary>
public class CountryIdConverter : ValueConverter<CountryId, Guid>
{
    public CountryIdConverter() : base(id => id.Value, guid => CountryId.Create(guid)) { }
}
