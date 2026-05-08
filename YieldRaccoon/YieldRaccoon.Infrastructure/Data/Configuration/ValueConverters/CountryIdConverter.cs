using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

/// <summary>
/// EF Core value converter for <see cref="CountryId"/> to/from <see cref="Guid"/>.
/// </summary>
public class CountryIdConverter : ValueConverter<CountryId, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountryIdConverter"/> class.
    /// </summary>
    public CountryIdConverter() : base(
        id => id.Value,
        guid => CountryId.Create(guid))
    {
    }
}
