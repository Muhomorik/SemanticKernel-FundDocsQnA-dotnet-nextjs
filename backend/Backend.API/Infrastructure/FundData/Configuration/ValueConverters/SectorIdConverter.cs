using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.API.Infrastructure.FundData.Configuration.ValueConverters;

/// <summary>EF Core value converter for <see cref="SectorId"/> ↔ <see cref="Guid"/>.</summary>
public class SectorIdConverter : ValueConverter<SectorId, Guid>
{
    public SectorIdConverter() : base(id => id.Value, guid => SectorId.Create(guid)) { }
}
