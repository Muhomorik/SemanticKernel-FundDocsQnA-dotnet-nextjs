namespace YieldRaccoon.Application.DTOs.Api;

/// <summary>
/// HTTP API DTO for a single full history record used in the full-sync path (POST /api/funds/full-sync).
/// Carries all time-varying fields from <c>FundHistoryRecord</c>, unlike
/// <see cref="ApiFundHistoryPointDto"/> which only carries Nav and NavDate.
/// </summary>
/// <remarks>
/// Mirror of Backend.API.ApplicationCore.DTOs.ApiFundFullHistoryRecordDto — same shape, own namespace.
/// <para>
/// Backend upsert semantics (via <c>UpsertSparseRangeAsync</c>):
/// <list type="bullet">
///   <item>Match by (Isin, NavDate) composite key.</item>
///   <item>New record: INSERT with all fields.</item>
///   <item>Existing record: update Capital/NumberOfOwners/Risk/SharpeRatio/StandardDeviation
///         only when the incoming value is non-null. Nav and NavDate are never modified.</item>
/// </list>
/// </para>
/// </remarks>
public sealed record ApiFundFullHistoryRecordDto
{
    public required string Isin { get; init; }

    /// <summary>NAV date in "yyyy-MM-dd" format. Used as the composite key together with Isin.</summary>
    public string? NavDate { get; init; }

    /// <summary>Net Asset Value. Never overwritten on existing records.</summary>
    public decimal? Nav { get; init; }

    public decimal? Capital { get; init; }
    public int? NumberOfOwners { get; init; }
    public int? Risk { get; init; }
    public decimal? SharpeRatio { get; init; }
    public decimal? StandardDeviation { get; init; }
}
