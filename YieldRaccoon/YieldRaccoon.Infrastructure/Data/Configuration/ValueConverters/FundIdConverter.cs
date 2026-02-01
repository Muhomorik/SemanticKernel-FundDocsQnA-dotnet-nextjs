using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

/// <summary>
/// EF Core value converter for <see cref="FundId"/> to/from string (ISIN).
/// </summary>
public class FundIdConverter : ValueConverter<FundId, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FundIdConverter"/> class.
    /// </summary>
    public FundIdConverter() : base(
        id => id.Isin,
        isin => FundId.Create(isin))
    {
    }
}
