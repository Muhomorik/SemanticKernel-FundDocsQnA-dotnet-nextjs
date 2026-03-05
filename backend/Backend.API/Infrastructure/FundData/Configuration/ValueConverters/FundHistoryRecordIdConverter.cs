using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.API.Infrastructure.FundData.Configuration.ValueConverters;

/// <summary>
/// EF Core value converter for <see cref="FundHistoryRecordId"/> to/from long.
/// </summary>
/// <remarks>
/// Uses the struct constructor directly instead of <see cref="FundHistoryRecordId.Create"/>
/// because EF Core uses temporary negative values as sentinels for identity columns
/// before the database assigns the actual ID.
/// </remarks>
public class FundHistoryRecordIdConverter : ValueConverter<FundHistoryRecordId, long>
{
    public FundHistoryRecordIdConverter() : base(
        id => id.Value,
        value => new FundHistoryRecordId(value))
    {
    }
}
