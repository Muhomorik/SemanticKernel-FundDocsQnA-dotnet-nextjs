using YieldRaccoon.Domain.Entities;

namespace YieldRaccoon.Application.Repositories;

/// <summary>
/// Repository for the <see cref="Sector"/> lookup table.
/// </summary>
/// <remarks>
/// Lookup rows grow organically — <see cref="GetOrCreateAsync"/> is the primary entry point.
/// </remarks>
public interface ISectorRepository
{
    /// <summary>
    /// Returns the existing <see cref="Sector"/> with the given display name, or inserts a new one
    /// (with a fresh GUID) and returns it. Tracks the new entity but does not call SaveChanges.
    /// </summary>
    /// <param name="displayName">Sector display name (natural key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Sector> GetOrCreateAsync(string displayName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a sector by display name without inserting if absent.
    /// </summary>
    Task<Sector?> FindByDisplayNameAsync(string displayName, CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
