using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

/// <summary>
/// EF Core value converter for <see cref="SectorId"/> to/from <see cref="Guid"/>.
/// </summary>
public class SectorIdConverter : ValueConverter<SectorId, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SectorIdConverter"/> class.
    /// </summary>
    public SectorIdConverter() : base(
        id => id.Value,
        guid => SectorId.Create(guid))
    {
    }
}
