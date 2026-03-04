namespace YieldRaccoon.Application.Models;

/// <summary>
/// Reports progress during a cloud sync operation.
/// </summary>
public sealed record CloudSyncProgress
{
    /// <summary>Total number of funds matched by the filter.</summary>
    public required int TotalFunds { get; init; }

    /// <summary>Number of funds processed so far.</summary>
    public required int ProcessedFunds { get; init; }

    /// <summary>Number of funds synced successfully.</summary>
    public required int SuccessCount { get; init; }

    /// <summary>Number of funds that failed to sync.</summary>
    public required int FailCount { get; init; }

    /// <summary>Name of the fund currently being synced.</summary>
    public required string CurrentFundName { get; init; }

    /// <summary>Human-readable phase description (e.g., "Querying...", "Syncing history (42/200)").</summary>
    public required string Phase { get; init; }
}
