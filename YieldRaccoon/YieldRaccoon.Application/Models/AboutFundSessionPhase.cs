namespace YieldRaccoon.Application.Models;

/// <summary>
/// Lifecycle phase of an about-fund browsing session.
/// </summary>
/// <remarks>
/// Mirrors <c>CollectionPhase</c> from the page-level collector
/// but at the session level (across multiple fund visits).
/// </remarks>
public enum AboutFundSessionPhase
{
    /// <summary>No session is active.</summary>
    Idle,

    /// <summary>Random delay countdown before the next fund navigation.</summary>
    DelayBeforeNavigation,

    /// <summary>Fund page visit in progress — collector is running interactions.</summary>
    Collecting,

    /// <summary>All funds visited, session finished.</summary>
    Completed
}
