using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Data.Configuration.ValueConverters;

/// <summary>
/// EF Core value converter for <see cref="FundHistoryRecordId"/> to/from long.
/// </summary>
/// <remarks>
/// Uses the struct constructor directly instead of <see cref="FundHistoryRecordId.Create"/>
/// because EF Core uses temporary negative values as sentinels for identity columns
/// before the database assigns the actual ID. The Create method's validation would
/// reject these temporary values.
/// </remarks>
public class FundHistoryRecordIdConverter : ValueConverter<FundHistoryRecordId, long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FundHistoryRecordIdConverter"/> class.
    /// </summary>
    public FundHistoryRecordIdConverter() : base(
        id => id.Value,
        value => new FundHistoryRecordId(value))
    {
    }
}
