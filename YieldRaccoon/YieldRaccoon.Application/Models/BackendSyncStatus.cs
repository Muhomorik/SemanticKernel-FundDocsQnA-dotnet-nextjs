namespace YieldRaccoon.Application.Models;

/// <summary>
/// Represents the outcome of a backend API sync operation for status bar display.
/// </summary>
public sealed record BackendSyncStatus
{
    /// <summary>
    /// Gets whether the sync operation succeeded.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets a human-readable status message for the status bar.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this status was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a success status with the given message.
    /// </summary>
    public static BackendSyncStatus Success(string message) =>
        new() { IsSuccess = true, Message = message };

    /// <summary>
    /// Creates an error status with the given message.
    /// </summary>
    public static BackendSyncStatus Error(string message) =>
        new() { IsSuccess = false, Message = message };
}
