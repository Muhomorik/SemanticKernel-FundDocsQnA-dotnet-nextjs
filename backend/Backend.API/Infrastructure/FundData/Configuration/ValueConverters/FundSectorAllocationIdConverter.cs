using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.API.Infrastructure.FundData.Configuration.ValueConverters;

/// <summary>EF Core value converter for <see cref="FundSectorAllocationId"/> ↔ <see cref="Guid"/>.</summary>
public class FundSectorAllocationIdConverter : ValueConverter<FundSectorAllocationId, Guid>
{
    public FundSectorAllocationIdConverter() : base(id => id.Value, guid => FundSectorAllocationId.Create(guid)) { }
}
