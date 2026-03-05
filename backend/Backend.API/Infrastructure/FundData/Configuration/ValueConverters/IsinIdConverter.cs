using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.API.Infrastructure.FundData.Configuration.ValueConverters;

/// <summary>
/// EF Core value converter for <see cref="IsinId"/> to/from string (ISIN).
/// </summary>
public class IsinIdConverter : ValueConverter<IsinId, string>
{
    public IsinIdConverter() : base(
        id => id.Isin,
        isin => IsinId.Create(isin))
    {
    }
}
