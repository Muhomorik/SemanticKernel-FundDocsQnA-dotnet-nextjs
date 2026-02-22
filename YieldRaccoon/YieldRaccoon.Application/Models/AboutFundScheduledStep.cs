namespace YieldRaccoon.Application.Models;

/// <summary>
/// A pre-calculated interaction step with the exact time it should fire.
/// </summary>
/// <remarks>
/// Part of the <see cref="AboutFundCollectionSchedule"/> built by the orchestrator.
/// The collector converts these to relative delays when scheduling timers.
/// </remarks>
/// <param name="Kind">The type of interaction.</param>
/// <param name="FireAt">Absolute time when this step should execute.</param>
public record AboutFundScheduledStep(
    AboutFundCollectionStepKind Kind,
    DateTimeOffset FireAt);
