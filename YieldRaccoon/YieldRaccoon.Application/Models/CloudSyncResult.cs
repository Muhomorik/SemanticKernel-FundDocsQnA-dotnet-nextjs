namespace YieldRaccoon.Application.Models;

/// <summary>
/// Final result of a cloud sync operation.
/// </summary>
public sealed record CloudSyncResult
{
    /// <summary>Total number of funds matched by the filter.</summary>
    public required int TotalFunds { get; init; }

    /// <summary>Number of fund profiles synced in the batch call.</summary>
    public required int ProfilesSynced { get; init; }

    /// <summary>Total number of history records inserted across all funds.</summary>
    public required int HistoryRecordsSynced { get; init; }

    /// <summary>Number of funds whose per-fund sync call failed.</summary>
    public required int FailedFunds { get; init; }

    /// <summary>Whether the operation was cancelled by the user.</summary>
    public required bool WasCancelled { get; init; }

    /// <summary>Total wall-clock duration of the sync operation.</summary>
    public required TimeSpan Duration { get; init; }
}
