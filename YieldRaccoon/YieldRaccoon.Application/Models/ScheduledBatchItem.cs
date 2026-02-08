using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Represents a single scheduled batch for UI display in the scheduled events list.
/// </summary>
/// <remarks>
/// <para>
/// Each item shows the batch number, scheduled time, current status, and optionally
/// how many funds were loaded (for completed batches).
/// </para>
/// </remarks>
[DebuggerDisplay("Batch {BatchNumber}: {Status} at {ScheduledAt:HH:mm:ss}")]
public sealed record ScheduledBatchItem
{
    /// <summary>
    /// Gets the batch number (1-based).
    /// </summary>
    public required BatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the scheduled time for this batch load.
    /// </summary>
    public required DateTimeOffset ScheduledAt { get; init; }

    /// <summary>
    /// Gets the current status of this batch.
    /// </summary>
    public required BatchStatus Status { get; init; }

    /// <summary>
    /// Gets the number of funds loaded in this batch, or null if not yet completed.
    /// </summary>
    public int? FundsLoaded { get; init; }

    /// <summary>
    /// Gets the time remaining until this batch is scheduled to load.
    /// </summary>
    /// <remarks>
    /// Returns null if the batch is not pending or the scheduled time has passed.
    /// </remarks>
    public TimeSpan? TimeUntilScheduled =>
        Status == BatchStatus.Pending && ScheduledAt > DateTimeOffset.UtcNow
            ? ScheduledAt - DateTimeOffset.UtcNow
            : null;
}

/// <summary>
/// Represents the current status of a scheduled batch.
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// The batch is scheduled but not yet started.
    /// </summary>
    Pending,

    /// <summary>
    /// The batch is currently being loaded.
    /// </summary>
    InProgress,

    /// <summary>
    /// The batch has been successfully loaded.
    /// </summary>
    Completed,

    /// <summary>
    /// The batch failed to load.
    /// </summary>
    Failed
}
