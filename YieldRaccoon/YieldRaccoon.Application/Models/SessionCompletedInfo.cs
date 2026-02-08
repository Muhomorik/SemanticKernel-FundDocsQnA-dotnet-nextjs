using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Information emitted when a crawl session completes successfully.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator emits this via observable stream when all funds have been loaded
/// and the session ends normally. Presentation layer can use this for completion
/// notifications or statistics display.
/// </para>
/// </remarks>
[DebuggerDisplay("Session {SessionId} completed: {TotalFundsLoaded} funds in {Duration}")]
public sealed record SessionCompletedInfo
{
    /// <summary>
    /// Gets the unique identifier of the completed session.
    /// </summary>
    public required CrawlSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the total number of funds loaded during the session.
    /// </summary>
    public required int TotalFundsLoaded { get; init; }

    /// <summary>
    /// Gets the total number of batches that were loaded.
    /// </summary>
    public required int TotalBatches { get; init; }

    /// <summary>
    /// Gets the total duration of the session from start to completion.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}
