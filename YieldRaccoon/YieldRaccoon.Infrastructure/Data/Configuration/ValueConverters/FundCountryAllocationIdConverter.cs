using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

/// <summary>
/// EF Core value converter for <see cref="FundCountryAllocationId"/> to/from <see cref="Guid"/>.
/// </summary>
public class FundCountryAllocationIdConverter : ValueConverter<FundCountryAllocationId, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FundCountryAllocationIdConverter"/> class.
    /// </summary>
    public FundCountryAllocationIdConverter() : base(
        id => id.Value,
        guid => FundCountryAllocationId.Create(guid))
    {
    }
}
