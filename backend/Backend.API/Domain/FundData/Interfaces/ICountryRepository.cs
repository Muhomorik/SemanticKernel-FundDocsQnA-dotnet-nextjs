using Backend.API.Domain.FundData.Models;

namespace Backend.API.Domain.FundData.Interfaces;

/// <summary>
/// Repository for the <see cref="Country"/> lookup table.
/// </summary>
public interface ICountryRepository
{
    /// <summary>
    /// Returns the existing <see cref="Country"/> with the given display name, or inserts a new one
    /// (with a fresh GUID). Backfills <see cref="Country.CountryCode"/> when existing is null and
    /// payload provides one. Tracks the change but does not save.
    /// </summary>
    Task<Country> GetOrCreateAsync(string displayName, string? countryCode,
        CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
