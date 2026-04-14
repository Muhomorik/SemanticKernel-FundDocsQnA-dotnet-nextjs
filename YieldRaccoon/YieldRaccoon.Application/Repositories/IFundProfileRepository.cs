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
    /// Gets fund profiles ordered by last visit date ascending (never-visited first),
    /// limited to a specified count.
    /// </summary>
    /// <remarks>
    /// Used by the about-fund browsing feature to prioritize funds with the oldest
    /// (or missing) visit data. Funds with null <c>AboutFundLastVisitedAt</c> sort first.
    /// <para>
    /// Results are pre-filtered: funds whose <c>CrawlerLastUpdatedAt</c> is null or
    /// older than one month are excluded, because the list crawler has effectively
    /// stopped seeing them (they are most likely delisted).
    /// </para>
    /// </remarks>
    /// <param name="limit">Maximum number of funds to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Funds ordered by last visit date ascending, never-visited first.</returns>
    Task<IReadOnlyList<AboutFundScheduleItem>> GetFundsOrderedByLastVisitAsync(
        int limit = 60, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the ISIN for a fund identified by its OrderBookId.
    /// </summary>
    /// <remarks>
    /// Used by manual collection mode to resolve the ISIN needed for chart data persistence
    /// when the fund may not be in the pre-loaded schedule.
    /// </remarks>
    /// <param name="orderBookId">The fund's OrderBookId from the external URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fund's ISIN string, or <c>null</c> if no matching profile was found.</returns>
    Task<string?> GetIsinByOrderBookIdAsync(OrderBookId orderBookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a fund profile by its ISIN identifier.
    /// </summary>
    /// <param name="isinId">The fund's ISIN identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fund profile, or <c>null</c> if not found.</returns>
    Task<FundProfile?> GetByIsinAsync(IsinId isinId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets fund profiles optionally filtered by company name, with history records eagerly loaded.
    /// Returns all profiles when <paramref name="companyName"/> is <c>null</c> or empty.
    /// </summary>
    /// <param name="companyName">Optional company name substring filter (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fund profiles with their history records.</returns>
    Task<IReadOnlyList<FundProfile>> GetByCompanyNameFilterAsync(
        string? companyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a fund profile exists for the given ISIN.
    /// </summary>
    /// <remarks>
    /// Used by chart ingestion to guard against FK violations when saving history records
    /// for a fund whose profile has not been crawled yet (a normal situation).
    /// </remarks>
    /// <param name="isinId">The fund's ISIN identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the profile exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsByIsinAsync(IsinId isinId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Updates the <see cref="FundProfile.Description"/> for the given fund.
    /// </summary>
    /// <remarks>
    /// Called by the about-fund orchestrator after extracting the description
    /// from the fund-reference API response.
    /// </remarks>
    /// <param name="isinId">The fund's ISIN identifier.</param>
    /// <param name="description">The fund description text, or <c>null</c> to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateDescriptionAsync(IsinId isinId, string? description, CancellationToken cancellationToken = default);
}
