using YieldRaccoon.Domain.Entities;

namespace YieldRaccoon.Application.Repositories;

/// <summary>
/// Repository for the <see cref="Country"/> lookup table.
/// </summary>
/// <remarks>
/// Lookup rows grow organically — <see cref="GetOrCreateAsync"/> is the primary entry point.
/// Direct add/save methods are also exposed for the diff-based ingestion service.
/// </remarks>
public interface ICountryRepository
{
    /// <summary>
    /// Returns the existing <see cref="Country"/> with the given display name, or inserts a new one
    /// (with a fresh GUID) and returns it. Tracks the new entity but does not call SaveChanges —
    /// the caller controls the transaction boundary.
    /// </summary>
    /// <remarks>
    /// On a re-encounter where <paramref name="countryCode"/> is non-null and the existing
    /// row's <see cref="Country.CountryCode"/> is null, the code is backfilled. Existing
    /// non-null codes are never overwritten with null.
    /// </remarks>
    /// <param name="displayName">Country display name (natural key).</param>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code (or <c>null</c> if unknown).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Country> GetOrCreateAsync(string displayName, string? countryCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a country by display name without inserting if absent.
    /// </summary>
    Task<Country?> FindByDisplayNameAsync(string displayName, CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
