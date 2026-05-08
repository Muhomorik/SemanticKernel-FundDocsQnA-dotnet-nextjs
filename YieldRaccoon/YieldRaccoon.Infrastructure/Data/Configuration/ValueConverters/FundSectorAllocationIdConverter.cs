using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

/// <summary>
/// EF Core value converter for <see cref="FundSectorAllocationId"/> to/from <see cref="Guid"/>.
/// </summary>
public class FundSectorAllocationIdConverter : ValueConverter<FundSectorAllocationId, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FundSectorAllocationIdConverter"/> class.
    /// </summary>
    public FundSectorAllocationIdConverter() : base(
        id => id.Value,
        guid => FundSectorAllocationId.Create(guid))
    {
    }
}
