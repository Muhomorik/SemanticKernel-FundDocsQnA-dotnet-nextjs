namespace YieldRaccoon.Application.Models;

/// <summary>
/// A single scheduled interaction step with its expected fire time and current status.
/// </summary>
/// <param name="Kind">The type of interaction.</param>
/// <param name="Delay">Cumulative delay from collection start when this step fires.</param>
/// <param name="Status">Current execution status of this step.</param>
public record AboutFundCollectionStep(
    AboutFundCollectionStepKind Kind,
    TimeSpan Delay,
    AboutFundCollectionStepStatus Status = AboutFundCollectionStepStatus.Pending);