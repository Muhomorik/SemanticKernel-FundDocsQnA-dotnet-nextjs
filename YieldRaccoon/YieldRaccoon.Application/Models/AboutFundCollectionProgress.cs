using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Live progress snapshot for a single fund page collection.
/// </summary>
/// <remarks>
/// Returned by <see cref="Services.IAboutFundPageDataCollector.BeginCollection"/> with initial state,
/// then emitted every second on <see cref="Services.IAboutFundPageDataCollector.StateChanged"/> with updated progress.
/// </remarks>
public record AboutFundCollectionProgress
{
    /// <summary>
    /// The OrderBookId for this collection.
    /// </summary>
    public required OrderBookId OrderBookId { get; init; }

    /// <summary>
    /// All scheduled interaction steps in execution order, with cumulative delays and statuses.
    /// </summary>
    public required IReadOnlyList<AboutFundCollectionStep> Steps { get; init; }

    /// <summary>
    /// Total duration including the safety-net timer — the hard deadline
    /// after which the collection is force-completed regardless of pending responses.
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Time elapsed since the collection started.
    /// </summary>
    public TimeSpan Elapsed { get; init; }

    /// <summary>
    /// Time remaining until the safety-net deadline.
    /// </summary>
    public TimeSpan Remaining { get; init; }

    /// <summary>
    /// Current slot data (fetch statuses and response previews).
    /// </summary>
    public required AboutFundPageData PageData { get; init; }
}
