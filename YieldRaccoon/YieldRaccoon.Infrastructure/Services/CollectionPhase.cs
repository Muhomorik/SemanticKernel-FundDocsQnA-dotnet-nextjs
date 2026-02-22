namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Tracks the collection lifecycle: idle → interacting → draining → completed.
/// </summary>
public enum CollectionPhase
{
    /// <summary>No collection in progress.</summary>
    Idle,

    /// <summary>Scheduled page interactions are firing (clicking buttons).</summary>
    Interacting,

    /// <summary>All interactions have fired; awaiting the final HTTP response.</summary>
    Draining,

    /// <summary>Collection finished — data emitted on <c>Completed</c>.</summary>
    Completed
}