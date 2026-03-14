using Backend.API.Domain.FundData.Models;

namespace Backend.API.Domain.FundData.Interfaces;

/// <summary>
/// Repository for <see cref="FundProfile"/> persistence operations.
/// </summary>
public interface IFundProfileRepository
{
    /// <summary>
    /// Upserts a fund profile: inserts if new, updates if existing (preserving FirstSeenAt).
    /// </summary>
    Task UpsertAsync(FundProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a fund profile only if no profile with the same ISIN already exists.
    /// Existing profiles are left completely untouched (no field updates).
    /// Used by the full-sync path to guarantee the FK exists without destroying existing profile data.
    /// </summary>
    Task InsertIfNotExistsAsync(FundProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the store.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
