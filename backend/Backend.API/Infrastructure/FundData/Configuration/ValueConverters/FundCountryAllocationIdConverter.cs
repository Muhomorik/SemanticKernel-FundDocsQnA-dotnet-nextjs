using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.API.Infrastructure.FundData.Configuration.ValueConverters;

/// <summary>EF Core value converter for <see cref="FundCountryAllocationId"/> ↔ <see cref="Guid"/>.</summary>
public class FundCountryAllocationIdConverter : ValueConverter<FundCountryAllocationId, Guid>
{
    public FundCountryAllocationIdConverter() : base(id => id.Value, guid => FundCountryAllocationId.Create(guid)) { }
}
