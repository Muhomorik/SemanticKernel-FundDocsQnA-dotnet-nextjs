namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// Request body for POST /api/funds/full-sync.
/// Carries static profile metadata (to guarantee the fund exists) plus the complete set
/// of history records with all time-varying fields.
/// </summary>
/// <remarks>
/// Used exclusively by <c>CloudSyncWindow</c> for bulk full-sync.
/// Not used by the DualWrite live-crawl path, which continues to use
/// <see cref="FundAboutSyncRequest"/> with <see cref="ApiFundHistoryPointDto"/>.
/// </remarks>
public sealed record FundFullHistorySyncRequest
{
    /// <summary>Static profile metadata. Used to guarantee the fund FK exists (insert-if-not-exists).</summary>
    public required ApiFundFullSyncProfileMetadataDto Profile { get; init; }

    /// <summary>Full history records for this fund. All time-varying fields included.</summary>
    public IReadOnlyList<ApiFundFullHistoryRecordDto> HistoryRecords { get; init; } = [];
}
