using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Ingests country and sector portfolio allocations from about-fund page visits into the
/// persistence layer as <see cref="Domain.Entities.FundCountryAllocation"/> and
/// <see cref="Domain.Entities.FundSectorAllocation"/> entities.
/// </summary>
/// <remarks>
/// <para>
/// Reads <c>PortfolioDataJson</c> from the page data (captured passively from the
/// <c>_api/fund-reference/portfolio-data/{orderBookId}</c> endpoint), deserializes it, and
/// performs a diff-based upsert: insert new (fund, country/sector) pairs, update existing
/// percentages, and delete pairs that disappeared from the latest payload.
/// </para>
/// <para>
/// Lookup rows in <see cref="Domain.Entities.Country"/> and <see cref="Domain.Entities.Sector"/>
/// grow organically — the first encounter inserts; subsequent encounters reuse the existing
/// row by display name.
/// </para>
/// </remarks>
public interface IPortfolioDataIngestionService
{
    /// <summary>
    /// Ingests country and sector allocations from a completed about-fund page visit.
    /// </summary>
    /// <param name="pageData">
    /// Page data containing the raw portfolio-data JSON (or <c>null</c> if the endpoint
    /// did not respond). Returns 0 in either case without raising.
    /// </param>
    /// <param name="isinId">
    /// The fund's ISIN. Used as the foreign key on persisted allocation rows.
    /// If no <see cref="Domain.Entities.FundProfile"/> exists for this ISIN, the call
    /// logs a warning and returns 0.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The total number of allocation rows touched (inserted + updated + deleted) across
    /// both countries and sectors. Returns <c>0</c> when the payload is missing, empty,
    /// or malformed.
    /// </returns>
    Task<int> IngestPortfolioDataAsync(
        AboutFundPageData pageData,
        IsinId isinId,
        CancellationToken cancellationToken = default);
}
