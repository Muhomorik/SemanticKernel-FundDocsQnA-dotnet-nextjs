using YieldRaccoon.Domain.Entities;

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
}
