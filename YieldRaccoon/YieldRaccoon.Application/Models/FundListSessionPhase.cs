namespace YieldRaccoon.Application.Models;

/// <summary>
/// Lifecycle phase of a fund list crawl session.
/// </summary>
/// <remarks>
/// The orchestrator transitions through these phases explicitly,
/// replacing the previous implicit derivation from event store queries.
/// </remarks>
public enum FundListSessionPhase
{
    /// <summary>No session is active.</summary>
    Idle,

    /// <summary>Random delay countdown before the next batch load.</summary>
    DelayBeforeNextBatch,

    /// <summary>Batch load in progress — clicked "Show more", awaiting response.</summary>
    Loading
}
