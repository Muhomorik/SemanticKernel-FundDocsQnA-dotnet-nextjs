using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Represents a countdown tick emitted every second during batch delay.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator emits these ticks via observable stream so the presentation
/// layer can update countdown displays without managing timers directly.
/// </para>
/// </remarks>
/// <param name="NextBatchNumber">The batch number that will load when countdown completes.</param>
/// <param name="SecondsRemaining">Number of seconds remaining until the batch loads.</param>
/// <param name="TotalDelay">Total duration of the delay for progress calculation.</param>
public readonly record struct CountdownTick(
    BatchNumber NextBatchNumber,
    int SecondsRemaining,
    TimeSpan TotalDelay);
