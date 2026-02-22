using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Ingests chart data from about-fund page visits into the persistence layer
/// as <see cref="Domain.Entities.FundHistoryRecord"/> entities.
/// </summary>
/// <remarks>
/// <para>
/// Takes the raw <see cref="AboutFundPageData"/> collected during a fund detail page visit
/// (containing JSON from up to 7 chart time periods), deserializes the data, merges overlapping
/// time series with deduplication by NAV date, and persists the resulting history records
/// via <see cref="Repositories.IFundHistoryRepository"/>.
/// </para>
/// <para>
/// Chart-derived records only contain <c>Nav</c> and <c>NavDate</c> fields.
/// Other <see cref="Domain.Entities.FundHistoryRecord"/> fields (Capital, NumberOfOwners,
/// Risk, SharpeRatio, StandardDeviation) are left <c>null</c> since chart data
/// does not carry those metrics.
/// </para>
/// </remarks>
public interface IAboutFundChartIngestionService
{
    /// <summary>
    /// Ingests chart data from a completed about-fund page visit.
    /// </summary>
    /// <param name="pageData">
    /// The collected page data containing raw JSON in each chart slot.
    /// Slots where <see cref="Domain.ValueObjects.AboutFundFetchSlot.IsSucceeded"/> is <c>false</c> are skipped.
    /// </param>
    /// <param name="isinId">
    /// The fund's ISIN identifier, resolved by the orchestrator from the session schedule.
    /// Used as the foreign key on persisted <see cref="Domain.Entities.FundHistoryRecord"/> entities.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The number of unique NAV-date records persisted (after deduplication across time periods).
    /// Returns <c>0</c> if no chart slots succeeded or all data was invalid.
    /// </returns>
    Task<int> IngestChartDataAsync(
        AboutFundPageData pageData,
        IsinId isinId,
        CancellationToken cancellationToken = default);
}
