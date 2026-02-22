namespace YieldRaccoon.Application.Models;

/// <summary>
/// Tracks the visit state of a single fund within a browsing session.
/// </summary>
public enum FundVisitStatus
{
    /// <summary>Fund has not been visited yet.</summary>
    NotVisited,

    /// <summary>Fund page is currently being collected.</summary>
    Collecting,

    /// <summary>Fund page visit is complete (data collected or force-completed).</summary>
    Completed
}
