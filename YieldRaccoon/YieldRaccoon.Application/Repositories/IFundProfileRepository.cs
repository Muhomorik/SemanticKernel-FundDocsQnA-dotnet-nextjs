using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Repositories;

/// <summary>
/// Repository interface for persistent fund profile storage.
/// </summary>
/// <remarks>
/// <para>
/// This repository manages <see cref="FundProfile"/> entities in the database.
/// It provides async-only operations for adding or updating fund profiles.
/// </para>
/// </remarks>
public interface IFundProfileRepository
{
    /// <summary>
    /// Adds or updates a fund profile asynchronously.
    /// If a profile with the same FundId exists, updates it; otherwise adds it.
    /// </summary>
    /// <param name="fundProfile">The fund profile to add or update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddOrUpdateAsync(FundProfile fundProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets fund profiles ordered by history record count (ascending), limited to a specified count.
    /// </summary>
    /// <remarks>
    /// Used by the about-fund browsing feature to find funds with the least historical data.
    /// </remarks>
    /// <param name="limit">Maximum number of funds to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Funds with their history record counts, ordered ascending.</returns>
    Task<IReadOnlyList<AboutFundScheduleItem>> GetFundsOrderedByHistoryCountAsync(
        int limit = 60, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the <see cref="FundProfile.AboutFundLastVisitedAt"/> timestamp for the given fund.
    /// </summary>
    /// <remarks>
    /// Performs a targeted single-column update without loading the full entity graph.
    /// Called by the about-fund orchestrator when a fund page visit completes.
    /// </remarks>
    /// <param name="isinId">The fund's ISIN identifier.</param>
    /// <param name="visitedAt">The timestamp of the visit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateLastVisitedAtAsync(IsinId isinId, DateTimeOffset visitedAt, CancellationToken cancellationToken = default);
}
