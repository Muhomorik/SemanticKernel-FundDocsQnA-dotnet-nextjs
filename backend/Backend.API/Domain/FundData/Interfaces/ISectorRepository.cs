using Backend.API.Domain.FundData.Models;

namespace Backend.API.Domain.FundData.Interfaces;

/// <summary>
/// Repository for the <see cref="Sector"/> lookup table.
/// </summary>
public interface ISectorRepository
{
    /// <summary>
    /// Returns the existing <see cref="Sector"/> with the given display name, or inserts a new one.
    /// Tracks the change but does not save.
    /// </summary>
    Task<Sector> GetOrCreateAsync(string displayName, CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
